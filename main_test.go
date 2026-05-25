package main

import "testing"

func TestDedicatedExploredFixture(t *testing.T) {
	decoded, err := readExploredFile("Dedicated.one_map_to_rule_them_all.explored")
	if err != nil {
		t.Fatal(err)
	}
	assertFixture(t, decoded, 533596, 120379, 325, 9300)
}

func TestDedicatedOldExploredFixture(t *testing.T) {
	decoded, err := readExploredFile("Dedicated.one_map_to_rule_them_all.explored.old")
	if err != nil {
		t.Fatal(err)
	}
	assertFixture(t, decoded, 1645822, 119706, 39255, 1121526)
}

func assertFixture(t *testing.T, decoded *DecodedFile, fileSize, exploredCount int, pinCount int32, estimatedPayloadBytes int) {
	t.Helper()
	if decoded.HeaderOffset != 0 {
		t.Fatalf("header offset = %d, want 0", decoded.HeaderOffset)
	}
	if decoded.Version != 2 {
		t.Fatalf("version = %d, want 2", decoded.Version)
	}
	if decoded.MapSize != mapSize {
		t.Fatalf("map size = %d, want %d", decoded.MapSize, mapSize)
	}
	if decoded.FileSize != fileSize {
		t.Fatalf("file size = %d, want %d", decoded.FileSize, fileSize)
	}
	if decoded.PackedMapBytes == nil || *decoded.PackedMapBytes != packedBytes {
		t.Fatalf("packed map bytes = %v, want %d", decoded.PackedMapBytes, packedBytes)
	}
	if decoded.FixedMapBytes != packedBytes {
		t.Fatalf("fixed map bytes = %d, want %d", decoded.FixedMapBytes, packedBytes)
	}
	if decoded.EstimatedPayloadBytes != estimatedPayloadBytes {
		t.Fatalf("estimated payload bytes = %d, want %d", decoded.EstimatedPayloadBytes, estimatedPayloadBytes)
	}
	if decoded.EstimatedPayloadBytes == decoded.FileSize {
		t.Fatalf("estimated payload bytes should exclude fixed packed map bytes")
	}
	if decoded.ExploredCount != exploredCount {
		t.Fatalf("explored count = %d, want %d", decoded.ExploredCount, exploredCount)
	}
	if decoded.PinCount != pinCount {
		t.Fatalf("pin count = %d, want %d", decoded.PinCount, pinCount)
	}
	if decoded.BytesConsumed != decoded.FileSize {
		t.Fatalf("bytes consumed = %d, want %d", decoded.BytesConsumed, decoded.FileSize)
	}
	if decoded.TrailingBytes != 0 {
		t.Fatalf("trailing bytes = %d, want 0", decoded.TrailingBytes)
	}
}
