using SpeechLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using NAudio.Wave;

namespace TtsMultilang {
	class TtsReadMultilang
	{
		private SpVoice vox;
		private string[] voiceNames;
		private string config_lang1 = null;
		private string config_lang2 = null;
		private int config_lang1_index = 0;
		private int config_lang2_index = 0;
		private int config_lang1_rate = 0;
		private int config_lang2_rate = 0;
		private string config_file_in = null;
		private string config_file_out = null;
		private Encoding config_file_encoding;
		private string config_tmpWav = null;
		private long config_maxBytesPerFile;

		public TtsReadMultilang() {
			init_vox();
			init_parseConfig();
			var filenamesTmp = readTheDamnFile();
			stitchTheWavsTogether(filenamesTmp);
		}

		private void init_vox() {
			vox = new SpVoice();
			var voices = vox.GetVoices();
			voiceNames = new string[voices.Count];
			for (int i = 0; i < voices.Count; i++) {
				var voice = voices.Item(i);
				var name = voice.GetDescription();
				voiceNames[i] = name;
			}
		}

		private void init_parseConfig() {
			config_file_in = ConfigurationManager.AppSettings["file_in"];
			config_file_out = ConfigurationManager.AppSettings["file_out"];
			config_file_encoding = Encoding.GetEncoding(ConfigurationManager.AppSettings["file_encoding"]);
			config_lang1 = ConfigurationManager.AppSettings["lang1"];
			config_lang2 = ConfigurationManager.AppSettings["lang2"];
			config_lang1_index = voiceNames.IndexOf(x => x.ContainsNoCase(ConfigurationManager.AppSettings["lang1_name"]));
			config_lang2_index = voiceNames.IndexOf(x => x.ContainsNoCase(ConfigurationManager.AppSettings["lang2_name"]));
			int.TryParse(ConfigurationManager.AppSettings["lang1_rate"], out config_lang1_rate);
			int.TryParse(ConfigurationManager.AppSettings["lang2_rate"], out config_lang2_rate);
			config_tmpWav = ConfigurationManager.AppSettings["tmpWav"] ?? "TtsReadMultilang.tmp.wav";
			long.TryParse(ConfigurationManager.AppSettings["maxBytesPerFile"], out config_maxBytesPerFile);
			if (config_maxBytesPerFile == 0)
				config_maxBytesPerFile = 50000000; // ~ 50 Mb
		}

		/// <summary>
		/// Returns all tmp filenames in the order they were written
		/// </summary>
		private List<string> readTheDamnFile() {
			var allLines = File.ReadAllLines(config_file_in, config_file_encoding);
			var filenamesTmp = new List<string>();
			var index = 0;
			var filenameOut = "";
			var languageIndex = 0;
			var rate = 0;
			string textToSay = null;
			foreach (var line in allLines) {
				if (String.IsNullOrWhiteSpace(line))
					continue;
				if (line.StartsWithNoCase(config_lang1 + ":")) {
					languageIndex = config_lang1_index;
					rate = config_lang1_rate;
					textToSay = line.Substring(config_lang1.Length + 1);
				}
				else if (line.StartsWithNoCase(config_lang2 + ":")) {
					languageIndex = config_lang2_index;
					rate = config_lang2_rate;
					textToSay = line.Substring(config_lang2.Length + 1);
				}
				// if the line does not begin with a language code (ru:, en:, es:, it:, etc) read with the previous voice
				// however, if there has not yet been any line that starts with language code, we don't really have a previous...
				else if (index == 0)
					continue;
				filenameOut = GetNumberedFilenameOut(config_tmpWav, index, 6);
				Console.WriteLine("speaking " + filenameOut);
				SayIt(languageIndex, rate, textToSay, filenameOut);
				filenamesTmp.Add(filenameOut);
				index++;

				// TMP debug!!!!!!!!!
				//if (index > 10)
				//	break;
			}
			return filenamesTmp;
		}

		private string GetNumberedFilenameOut(string template, int index, int zerosPad) {
			return template.Replace(".wav", String.Format("_{0:D" + zerosPad + "}.wav", index));
		}

		private void SayIt(int languageIndex, int rate, string textToSay, string filenameOut) {
			vox.Voice = vox.GetVoices().Item(languageIndex);
			vox.Rate = rate;
			try {
				var options = SpeechVoiceSpeakFlags.SVSFlagsAsync;
				var stream = new SpFileStream();
				stream.Open(filenameOut, SpeechStreamFileMode.SSFMCreateForWrite, true);
				vox.AudioOutputStream = stream;
				vox.Speak(textToSay, options);
				vox.WaitUntilDone(100000);
				stream.Close();
			}
			catch (Exception ex) { }
		}

		private void stitchTheWavsTogether(List<string> filenamesTmp) {
			var iTmp = 0;
			var iFile = 0;
			while (iTmp < filenamesTmp.Count) {
				var filenameOut = GetNumberedFilenameOut(config_file_out, iFile + 1, 3);
				Console.WriteLine("Concatenating into " + Path.GetFileName(filenameOut));
				iTmp += ConcatenateWavs(filenameOut, filenamesTmp, iTmp);
				iFile++;
			}
			Console.WriteLine("Deleting temp files...");
			// clean up... delete tmp files
			foreach (var tmp in filenamesTmp) {
				File.Delete(tmp);
			}
			Console.WriteLine("Done.");
		}

		/// <summary>
		/// from: http://stackoverflow.com/questions/6777340/how-to-join-2-or-more-wav-files-together-programatically
		/// modified: takes startIndex in sourceFiles array and returns how many it concatenated before the limit was exceeded
		/// </summary>
		private int ConcatenateWavs(string outputFile, List<string> sourceFiles, int startIndex) {
			byte[] buffer = new byte[1024];
			WaveFileWriter waveFileWriter = null;
			int howManyConcatenated = 0;
			long totalBytes = 0;

			try {
				for (int i = startIndex; i < sourceFiles.Count; i++) {
					var sourceFile = sourceFiles[i];
					using (WaveFileReader reader = new WaveFileReader(sourceFile)) {
						if (waveFileWriter == null) {
							// first time in create new Writer
							waveFileWriter = new WaveFileWriter(outputFile, reader.WaveFormat);
						}
						else {
							if (!reader.WaveFormat.Equals(waveFileWriter.WaveFormat)) {
								throw new InvalidOperationException("Can't concatenate WAV Files that don't share the same format");
							}
						}

						int read;
						while ((read = reader.Read(buffer, 0, buffer.Length)) > 0) {
							waveFileWriter.WriteData(buffer, 0, read);
							totalBytes += read;
						}
						howManyConcatenated++;
					}
					if (totalBytes > config_maxBytesPerFile)
						break;
				}
			}
			finally {
				if (waveFileWriter != null) {
					waveFileWriter.Dispose();
				}
			}
			return howManyConcatenated;
		}
	}
}
