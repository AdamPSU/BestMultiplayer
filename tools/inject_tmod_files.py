#!/usr/bin/env python3
"""Inject files into a packaged .tmod (avoids tML icon_small stream bug during package)."""

from __future__ import annotations

import argparse
import hashlib
import struct
import zlib
from pathlib import Path


def read_string(buf: bytes, j: int) -> tuple[str, int]:
	n = 0
	shift = 0
	while True:
		b = buf[j]
		j += 1
		n |= (b & 0x7F) << shift
		if b < 0x80:
			break
		shift += 7
	return buf[j : j + n].decode("utf-8"), j + n


def write_string(s: str) -> bytes:
	raw = s.encode("utf-8")
	n = len(raw)
	out = bytearray()
	while True:
		byte = n & 0x7F
		n >>= 7
		if n:
			out.append(byte | 0x80)
		else:
			out.append(byte)
			break
	out.extend(raw)
	return bytes(out)


def write_i32(n: int) -> bytes:
	return struct.pack("<i", n)


def write_u32(n: int) -> bytes:
	return struct.pack("<I", n)


def compress_entry(name: str, data: bytes) -> tuple[int, bytes]:
	# Match tML ShouldCompress: skip formats that deflate poorly.
	if len(data) <= 1024 or name.endswith((".png", ".mp3", ".ogg", ".rawimg")):
		return len(data), data
	# .NET DeflateStream = raw deflate (no zlib wrapper).
	co = zlib.compressobj(9, zlib.DEFLATED, -15)
	comp = co.compress(data) + co.flush()
	if len(comp) < len(data) * 0.9:
		return len(data), comp
	return len(data), data


def parse_tmod(path: Path) -> tuple[str, bytes, bytes, str, str, list[tuple[str, int, bytes]]]:
	data = path.read_bytes()
	if data[:4] != b"TMOD":
		raise SystemExit(f"not a tmod: {path}")
	ver, i = read_string(data, 4)
	file_hash = data[i : i + 20]
	i += 20
	sig = data[i : i + 256]
	i += 256
	datalen = struct.unpack_from("<I", data, i)[0]
	i += 4
	payload = data[i : i + datalen]
	j = 0
	name, j = read_string(payload, j)
	mod_ver, j = read_string(payload, j)
	count = struct.unpack_from("<i", payload, j)[0]
	j += 4
	table: list[tuple[str, int, int]] = []
	for _ in range(count):
		fn, j = read_string(payload, j)
		length = struct.unpack_from("<i", payload, j)[0]
		j += 4
		clen = struct.unpack_from("<i", payload, j)[0]
		j += 4
		table.append((fn, length, clen))
	files: list[tuple[str, int, bytes]] = []
	off = j
	for fn, length, clen in table:
		blob = payload[off : off + clen]
		off += clen
		files.append((fn, length, blob))
	return ver, file_hash, sig, name, mod_ver, files


def write_tmod(
	path: Path,
	tml_ver: str,
	sig: bytes,
	name: str,
	mod_ver: str,
	files: list[tuple[str, int, bytes]],
) -> None:
	# Build payload: name, version, table, file bytes
	body = bytearray()
	body.extend(write_string(name))
	body.extend(write_string(mod_ver))
	body.extend(write_i32(len(files)))
	for fn, length, blob in files:
		body.extend(write_string(fn))
		body.extend(write_i32(length))
		body.extend(write_i32(len(blob)))
	for _, _, blob in files:
		body.extend(blob)

	# Header with placeholder hash/sig/len, then fill hash over payload region.
	header = bytearray()
	header.extend(b"TMOD")
	header.extend(write_string(tml_ver))
	hash_pos = len(header)
	header.extend(b"\x00" * 20)  # hash
	header.extend(sig[:256].ljust(256, b"\x00"))
	header.extend(write_u32(len(body)))
	data_pos = len(header)
	out = bytes(header) + bytes(body)
	digest = hashlib.sha1(out[data_pos:]).digest()
	out = out[:hash_pos] + digest + out[hash_pos + 20 :]
	path.write_bytes(out)


def main() -> None:
	ap = argparse.ArgumentParser()
	ap.add_argument("tmod", type=Path)
	ap.add_argument(
		"--add",
		action="append",
		default=[],
		metavar="NAME=PATH",
		help="Add/replace a file inside the tmod, e.g. icon_small.png=./icon_small.png",
	)
	ap.add_argument(
		"--remove-glob-suffix",
		action="append",
		default=[],
		help="Remove entries whose names end with this suffix (e.g. .bak)",
	)
	args = ap.parse_args()

	tml_ver, _old_hash, sig, name, mod_ver, files = parse_tmod(args.tmod)
	by_name = {fn: (fn, length, blob) for fn, length, blob in files}

	for suffix in args.remove_glob_suffix:
		for fn in list(by_name):
			if fn.endswith(suffix):
				del by_name[fn]

	for spec in args.add:
		if "=" not in spec:
			raise SystemExit(f"bad --add {spec!r}, expected NAME=PATH")
		entry_name, src = spec.split("=", 1)
		data = Path(src).read_bytes()
		length, blob = compress_entry(entry_name, data)
		by_name[entry_name] = (entry_name, length, blob)

	ordered = list(by_name.values())
	write_tmod(args.tmod, tml_ver, sig, name, mod_ver, ordered)
	print(f"updated {args.tmod} ({len(ordered)} files)")


if __name__ == "__main__":
	main()
