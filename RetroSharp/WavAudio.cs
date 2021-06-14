using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace RetroSharp
{
    /// <summary>
    /// Contains sound information from a wav file.
    /// </summary>
    public class WavAudio
    {
        private byte[] data;
        private int nrChannels;
        private int sampleRate;
        private int byteRate;
        private int blockAlign;
        private int bitsPerSample;

        internal WavAudio()
        {
        }

        /// <summary>
        /// Loads a wav file from a stream.
        /// </summary>
        /// <param name="Input">Stream</param>
        /// <returns>Loaded Wav file.</returns>
        /// <exception cref="IOException">If the input is not a valid WAVE File</exception>
        public static WavAudio FromStream(Stream Input)
        {
            if (Input.Length - Input.Position < 12)
                throw new IOException("Invalid WAVE file.");

            WavAudio Result = new WavAudio();

            using (BinaryReader rd = new BinaryReader(Input))
            {
                // File Format documented here: https://ccrma.stanford.edu/courses/422/projects/WaveFormat/

                int ChunkID = rd.ReadInt32();   // 46464952 = FFIR ( = RIFF backwards)
                int ChunkSize = rd.ReadInt32();
                int Format = rd.ReadInt32();    // 45564157 = EVAW ( = WAVE backwards)

                if (ChunkID != 0x46464952 || Format != 0x45564157)
                    throw new IOException("Invalid WAVE file.");

                int SubChunk1ID = rd.ReadInt32();
                int SubChunk1Size = rd.ReadInt32();
                ushort AudioFormat = rd.ReadUInt16();

                if (AudioFormat != 1)
                    throw new Exception("Only uncompressed WAVE files (in PCM format) are supported.");

                Result.nrChannels = rd.ReadUInt16();
                Result.sampleRate = rd.ReadInt32();
                Result.byteRate = rd.ReadInt32();
                Result.blockAlign = rd.ReadUInt16();
                Result.bitsPerSample = rd.ReadUInt16();

                int SubChunk2ID = rd.ReadInt32();
                int SubChunk2Size = rd.ReadInt32();

                Result.data = new byte[SubChunk2Size];

                rd.Read(Result.data, 0, SubChunk2Size);
            }

            return Result;
        }

        /// <summary>
        /// Loads a wav file from its binary representation.
        /// </summary>
        /// <param name="Input">Binary data</param>
        /// <returns>Loaded Wav file.</returns>
        /// <exception cref="IOException">If the input is not a valid WAVE File</exception>
        public static WavAudio FromBinary(byte[] Input)
        {
            using (MemoryStream ms = new MemoryStream(Input))
            {
                return FromStream(ms);
            }
        }

        /// <summary>
        /// Loads a wav file from a file.
        /// </summary>
        /// <param name="FileName">File name to wav file.</param>
        /// <returns>Loaded Wav file.</returns>
        /// <exception cref="Exception">If the file was not found.</exception>
        /// <exception cref="IOException">If the input is not a valid WAVE File</exception>
        public static WavAudio FromFile(string FileName)
        {
            using (FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return FromStream(fs);
            }
        }

        /// <summary>
        /// Loads a wav file from an embedded resource.
        /// </summary>
        /// <param name="ResourceName">Name of the resource, relative to the namespace of the caller.</param>
        /// <returns>Loaded Wav file.</returns>
        /// <exception cref="Exception">If the resource was not found.</exception>
        /// <exception cref="IOException">If the input is not a valid WAVE File</exception>
        public static WavAudio FromResource(string ResourceName)
        {
            StackFrame StackFrame = new StackFrame(1);
            Type Caller = StackFrame.GetMethod().ReflectedType;
            Assembly Assembly = Caller.Assembly;
            string Namespace = Caller.Namespace;

            ResourceName = Namespace + "." + ResourceName;

            Stream Stream = Assembly.GetManifestResourceStream(ResourceName);
            if (Stream is null)
                throw RetroApplication.ResourceNotFoundException(ResourceName, Assembly);

            Stream.Position = 0;

            return FromStream(Stream);
        }

        /// <summary>
        /// Sound data
        /// </summary>
        public byte[] Data { get { return this.data; } }

        /// <summary>
        /// Number of channels. 1 = Mono, 2 = Stereo, etc.
        /// </summary>
        public int NrChannels { get { return this.nrChannels; } }

        /// <summary>
        /// Samples per second. (Hz)
        /// </summary>
        public int SampleRate { get { return this.sampleRate; } }

        /// <summary>
        /// Number of bytes of data used per second for entire audio.
        /// </summary>
        public int ByteRate { get { return this.byteRate; } }

        /// <summary>
        /// Number of bytes of data used per sample for all channels.
        /// </summary>
        public int BlockAlign { get { return this.blockAlign; } }

        /// <summary>
        /// Bits per sample.
        /// </summary>
        public int BitsPerSample { get { return this.bitsPerSample; } }

    }
}