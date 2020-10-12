# NDecrypt

A simple tool for simple people.

## What is this?

This is a code port of 3 different programs:

- `3ds_encrypt.py`
- `3ds_decrypt.py`
- `woodsec` (part of [wooddumper](https://github.com/TuxSH/wooddumper))

## No really, what is this?

This tool allows you to encrypt and decrypt your personally dumped NDS and N3DS roms with minimal hassle. The only caveat right now is that you need a `keys.bin` file for your personally obtained encryption keys.

## So how do I use this?

	NDecrypt.exe <flag> [-dev] <file|dir> ...

	Possible values for <flag>:
	encrypt, e - Encrypt the incoming files
	decrypt, d - Decrypt the incoming files


**Note:** This overwrites the incoming files, so make backups if you're working on your original, personal dumps.

**Note:** Mixed folders or inputs are also accepted, you can decrypt or encrypt multiple files, regardless of their type. This being said, you can only do encrypt OR decrypt at one time.

## I feel like something is missing...

You are! In fact, you may be asking, "Hey, what was that `keys.bin` you mentioned??". I'm glad you asked. It's used only for N3DS files. Since some people don't like reading code, you need the 9 16-bit keys in little endian format (most common extraction methods produce big endian, so keep that in mind). It's recommended that you fill with 0x00 if you don't have access to a particular value so it doesn't mess up the read. They need to be in the following order:

- Hardware constant
- KeyX0x10
- KeyX0x1B
- KeyX0x25
- KeyX0x2C
- DevKeyX0x10
- DevKeyX0x1B
- DevKeyX0x25
- DevKeyX0x2C

The last 4 are only required if you use the `-dev` flag. Once again, don't ask for these, please. If you're missing a required key, then things won't work. Don't blame me, blame society. Or something.

## Anything else?

I'd like to thank the developers of the original programs for doing the actual hard work to figure things out. I'd also like to thank everyone who helped to test this against the original programs and made code suggestions.

Unofficially, this is entirely, 100% FOSS, no strings attached. I keep forgetting what license that is.

## Disclaimer

This program is **ONLY** for use with personally dumped files and keys and is not meant for enabling illegal activity. I do not condone using this program for anything other than personal use and research. If this program is used for anything other than that, I cannot be held liable for anything that happens.