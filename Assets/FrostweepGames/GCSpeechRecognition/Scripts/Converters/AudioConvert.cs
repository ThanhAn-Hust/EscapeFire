using UnityEngine;

namespace FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.Tools
{
	public static class AudioConvert
	{
		private static string Convert(AudioClip clip, bool increaseVolume = false, float volume = 1f)
		{
			byte[] audioArray;

			if (increaseVolume)
			{
				clip.SetData(AudioClip2ByteConverter.ByteToFloat(
					AudioClip2ByteConverter.AudioClipToByte(clip, increaseVolume, volume)), 0);
			}


			audioArray = AudioClip2PCMConverter.AudioClip2PCM(clip);
			return System.Convert.ToBase64String(audioArray);
		}

		private static string Convert(float[] raw, int channels, bool increaseVolume = false, float volume = 1f)
		{
			byte[] audioArray;

			if (increaseVolume)
			{
				raw = AudioClip2ByteConverter.ByteToFloat(AudioClipRaw2ByteConverter.AudioClipRawToByte(raw, increaseVolume, volume));
			}

			audioArray = AudioClipRaw2PCMConverter.AudioClipRaw2PCM(raw, channels);
			return System.Convert.ToBase64String(audioArray);
		}

		public static AudioClip Convert(float[] samples, int channels = 2, int sampleRate = 16000)
		{
			AudioClip clip = AudioClip.Create($"AudioClip_{sampleRate}", samples.Length, channels, sampleRate, false);
			clip.SetData(samples, 0);
			return clip;
		}

		public static string ToBase64(this AudioClip clip, bool increaseVolume = false, float volume = 1f)
		{
			return Convert(clip, increaseVolume, volume);
		}

		public static string ToBase64(this float[] rawAudioClipData, int channels = 1, bool increaseVolume = false, float volume = 1f)
		{
			return Convert(rawAudioClipData, channels, increaseVolume, volume);
		}
	}
}