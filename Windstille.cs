using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using EFT.Weather;
using HarmonyLib;
using UnityEngine;

namespace Windstille
{
	// Das Spiel klemmt die Ziellautstaerke der Windschleife hart auf
	// mindestens 0,2 fest (WindAmbientBlender.method_7 ->
	// Mathf.Clamp(..., 0.2f, 1f)). Bei Windstaerke 0 rauscht es deshalb
	// weiter. Dieses Plugin zieht die Lautstaerke nach dem Takt des
	// Blenders wieder herunter - abhaengig vom echten Windwert.
	[BepInPlugin("com.kaan.windstille", "Windstille", "1.0.0")]
	[BepInDependency("com.SPT.core")]
	public class MainPlugin : BaseUnityPlugin
	{
		internal static ConfigEntry<bool> Eingeschaltet;
		internal static ConfigEntry<float> Schwelle;
		internal static ConfigEntry<float> LautstaerkeFlaute;
		internal static ConfigEntry<float> LautstaerkeWind;
		internal static ConfigEntry<bool> Protokoll;

		internal static FieldInfo FeldQuelle;
		internal static FieldInfo FeldMischQuelle;

		private void Awake()
		{
			Eingeschaltet = Config.Bind("1. Allgemein", "Eingeschaltet", true,
				"Daempft die Wind-Umgebungsschleife.");
			Schwelle = Config.Bind("2. Werte", "Schwelle fuer Flaute", 0.15f,
				new ConfigDescription("Bis zu welcher Windstaerke es als windstill gilt. Der Windwert des Spiels laeuft von 0 bis 1.",
					new AcceptableValueRange<float>(0f, 1f), Array.Empty<object>()));
			LautstaerkeFlaute = Config.Bind("2. Werte", "Lautstaerke bei Flaute", 0f,
				new ConfigDescription("0 = voellig still. Das Spiel selbst laesst nie unter 0,2 zu.",
					new AcceptableValueRange<float>(0f, 1f), Array.Empty<object>()));
			LautstaerkeWind = Config.Bind("2. Werte", "Lautstaerke bei Wind", 1f,
				new ConfigDescription("Obergrenze, wenn tatsaechlich Wind weht. 1 = unveraendert.",
					new AcceptableValueRange<float>(0f, 1f), Array.Empty<object>()));
			Protokoll = Config.Bind("3. Anzeige", "Ins Protokoll schreiben", false,
				"Schreibt die errechnete Lautstaerke mit. Nur zur Fehlersuche.");

			Type blender = AccessTools.TypeByName("Audio.AmbientSubsystem.WindAmbientBlender");
			if (blender == null)
			{
				Logger.LogError("[Windstille] Klasse WindAmbientBlender nicht gefunden - das Plugin tut nichts.");
				return;
			}
			FeldQuelle = AccessTools.Field(blender, "_windSource");
			FeldMischQuelle = AccessTools.Field(blender, "_windMixSource");
			MethodInfo takt = AccessTools.Method(blender, "ManualUpdate");
			if (FeldQuelle == null || takt == null)
			{
				Logger.LogError("[Windstille] _windSource oder ManualUpdate fehlen - EFT hat die Klasse geaendert.");
				return;
			}
			new Harmony("com.kaan.windstille").Patch(takt,
				postfix: new HarmonyMethod(typeof(Daempfer), nameof(Daempfer.Nachher)));
			Logger.LogInfo("[Windstille] Aktiv.");
		}
	}

	internal static class Daempfer
	{
		private static float _zuletzt = -1f;

		public static void Nachher(object __instance)
		{
			if (!MainPlugin.Eingeschaltet.Value || __instance == null)
			{
				return;
			}
			try
			{
				float staerke = 0f;
				WeatherController w = WeatherController.Instance;
				if (w != null)
				{
					staerke = w.WeatherCurve.Wind.magnitude;
				}

				float grenze;
				float schwelle = MainPlugin.Schwelle.Value;
				if (staerke <= schwelle)
				{
					grenze = MainPlugin.LautstaerkeFlaute.Value;
				}
				else
				{
					// ab der Schwelle sanft auf die volle Lautstaerke hochziehen,
					// damit es keinen hoerbaren Sprung gibt
					float anteil = (schwelle >= 1f) ? 1f : ((staerke - schwelle) / (1f - schwelle));
					grenze = Mathf.Lerp(MainPlugin.LautstaerkeFlaute.Value, MainPlugin.LautstaerkeWind.Value,
						Mathf.Clamp01(anteil));
				}

				// Nur deckeln, nie hochsetzen und nie multiplizieren - sonst
				// wuerde sich der Wert Bild fuer Bild gegen null schaukeln.
				Deckeln(MainPlugin.FeldQuelle, __instance, grenze);
				Deckeln(MainPlugin.FeldMischQuelle, __instance, grenze);

				if (MainPlugin.Protokoll.Value && Mathf.Abs(grenze - _zuletzt) > 0.01f)
				{
					_zuletzt = grenze;
					Debug.Log($"[Windstille] Windstaerke {staerke:0.00} -> Obergrenze {grenze:0.00}");
				}
			}
			catch
			{
			}
		}

		private static void Deckeln(FieldInfo feld, object halter, float grenze)
		{
			if (feld == null)
			{
				return;
			}
			AudioSource quelle = feld.GetValue(halter) as AudioSource;
			if (quelle != null && quelle.volume > grenze)
			{
				quelle.volume = grenze;
			}
		}
	}
}
