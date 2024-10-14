using System;
using System.IO;
using System.Linq;
using System.Numerics;
using SabreTools.IO.Extensions;
using SabreTools.IO.Readers;

namespace NDecrypt.N3DS
{
    public class DecryptArgs
    {
        #region Common Fields

        /// <summary>
        /// Represents if all of the keys have been initialized properly
        /// </summary>
        public bool? IsReady { get; private set; }

        #endregion

        #region 3DS-Specific Fields

        /// <summary>
        /// AES Hardware Constant
        /// </summary>
        public BigInteger AESHardwareConstant { get; private set; }

        #region Retail Keys

        /// <summary>
        /// KeyX 0x18 (New 3DS 9.3)
        /// </summary>
        public BigInteger KeyX0x18 { get; private set; }

        /// <summary>
        /// KeyX 0x1B (New 3DS 9.6)
        /// </summary>
        public BigInteger KeyX0x1B { get; private set; }

        /// <summary>
        /// KeyX 0x25 (> 7.x)
        /// </summary>
        public BigInteger KeyX0x25 { get; private set; }

        /// <summary>
        /// KeyX 0x2C (< 6.x)
        /// </summary>
        public BigInteger KeyX0x2C { get; private set; }

        #endregion

        #region Development Keys

        /// <summary>
        /// Dev KeyX 0x18 (New 3DS 9.3)
        /// </summary>
        public BigInteger DevKeyX0x18 { get; private set; }

        /// <summary>
        /// Dev KeyX 0x1B New 3DS 9.6)
        /// </summary>
        public BigInteger DevKeyX0x1B { get; private set; }

        /// <summary>
        /// Dev KeyX 0x25 (> 7.x)
        /// </summary>
        public BigInteger DevKeyX0x25 { get; private set; }

        /// <summary>
        /// Dev KeyX 0x2C (< 6.x)
        /// </summary>
        public BigInteger DevKeyX0x2C { get; private set; }

        #endregion

        #endregion

        /// <summary>
        /// Setup all of the necessary constants
        /// </summary>
        /// <param name="keyfile">Path to the keyfile</param>
        /// <param name="useAesKeysTxt">Indicates if the keyfile format is aeskeys.txt</param>
        public DecryptArgs(string? keyfile, bool useAesKeysTxt)
        {
            // Read the proper keyfile format
            if (useAesKeysTxt)
                InitAesKeysTxt(keyfile);
            else
                InitKeysBin(keyfile);
        }

        /// <summary>
        /// Setup all of the necessary constants from aes_keys.txt
        /// </summary>
        /// <param name="keyfile">Path to aes_keys.txt</param>
        private void InitAesKeysTxt(string? keyfile)
        {
            if (keyfile == null || !File.Exists(keyfile))
            {
                IsReady = false;
                return;
            }

            try
            {
                using var reader = new IniReader(keyfile);

                // This is required to preserve sign for BigInteger
                byte[] signByte = [0x00];

                while (reader.ReadNextLine())
                {
                    // Ignore comments in the file
                    if (reader.RowType == IniRowType.Comment)
                        continue;
                    if (reader.KeyValuePair == null || string.IsNullOrWhiteSpace(reader.KeyValuePair?.Key))
                        break;

                    var kvp = reader.KeyValuePair!.Value;
                    byte[] value = StringToByteArray(kvp.Value).Reverse().ToArray();
                    byte[] valueWithSign = value.Concat(signByte).ToArray();

                    switch (kvp.Key)
                    {
                        // Hardware constant
                        case "generator":
                            AESHardwareConstant = new BigInteger(value);
                            break;

                        // Retail Keys
                        case "slot0x18KeyX":
                            KeyX0x18 = new BigInteger(valueWithSign);
                            break;
                        case "slot0x1BKeyX":
                            KeyX0x1B = new BigInteger(valueWithSign);
                            break;
                        case "slot0x25KeyX":
                            KeyX0x25 = new BigInteger(valueWithSign);
                            break;
                        case "slot0x2CKeyX":
                            KeyX0x2C = new BigInteger(valueWithSign);
                            break;

                        // Currently Unused KeyX
                        case "slot0x03KeyX":
                        case "slot0x19KeyX":
                        case "slot0x1AKeyX":
                        case "slot0x1CKeyX":
                        case "slot0x1DKeyX":
                        case "slot0x1EKeyX":
                        case "slot0x1FKeyX":
                        case "slot0x2DKeyX":
                        case "slot0x2EKeyX":
                        case "slot0x2FKeyX":
                        case "slot0x30KeyX":
                        case "slot0x31KeyX":
                        case "slot0x32KeyX":
                        case "slot0x33KeyX":
                        case "slot0x34KeyX":
                        case "slot0x35KeyX":
                        case "slot0x36KeyX":
                        case "slot0x37KeyX":
                        case "slot0x38KeyX":
                        case "slot0x3AKeyX":
                        case "slot0x3BKeyX":
                            break;

                        // Currently Unused KeyY
                        case "slot0x03KeyY":
                        case "slot0x06KeyY":
                        case "slot0x07KeyY":
                        case "slot0x2EKeyY":
                        case "slot0x2FKeyY":
                        case "slot0x31KeyY":
                            break;

                        // Currently Unused KeyN
                        case "slot0x0DKeyN":
                        case "slot0x15KeyN":
                        case "slot0x16KeyN":
                        case "slot0x19KeyN":
                        case "slot0x1AKeyN":
                        case "slot0x1BKeyN":
                        case "slot0x1CKeyN":
                        case "slot0x1DKeyN":
                        case "slot0x1EKeyN":
                        case "slot0x1FKeyN":
                        case "slot0x24KeyN":
                        case "slot0x2DKeyN":
                        case "slot0x2EKeyN":
                        case "slot0x2FKeyN":
                        case "slot0x31KeyN":
                        case "slot0x32KeyN":
                        case "slot0x36KeyN":
                        case "slot0x37KeyN":
                        case "slot0x38KeyN":
                        case "slot0x3BKeyN":
                            break;
                    }
                }
            }
            catch
            {
                IsReady = false;
                return;
            }

            IsReady = true;
        }

        /// <summary>
        /// Setup all of the necessary constants from keys.bin
        /// </summary>
        /// <param name="keyfile">Path to keys.bin</param>
        /// <remarks>keys.bin should be in little endian format</remarks>
        private void InitKeysBin(string? keyfile)
        {
            if (keyfile == null || !File.Exists(keyfile))
            {
                IsReady = false;
                return;
            }

            try
            {
                using Stream reader = File.Open(keyfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // This is required to preserve sign for BigInteger
                byte[] signByte = [0x00];

                // Hardware constant
                AESHardwareConstant = new BigInteger(reader.ReadBytes(16));

                // Retail keys
                KeyX0x18 = new BigInteger([.. reader.ReadBytes(16), .. signByte]);
                KeyX0x1B = new BigInteger([.. reader.ReadBytes(16), .. signByte]);
                KeyX0x25 = new BigInteger([.. reader.ReadBytes(16), .. signByte]);
                KeyX0x2C = new BigInteger([.. reader.ReadBytes(16), .. signByte]);

                // Development keys
                DevKeyX0x18 = new BigInteger([.. reader.ReadBytes(16), .. signByte]);
                DevKeyX0x1B = new BigInteger([.. reader.ReadBytes(16), .. signByte]);
                DevKeyX0x25 = new BigInteger([.. reader.ReadBytes(16), .. signByte]);
                DevKeyX0x2C = new BigInteger([.. reader.ReadBytes(16), .. signByte]);
            }
            catch
            {
                IsReady = false;
                return;
            }

            IsReady = true;
        }

        // https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
        private static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }
    }
}