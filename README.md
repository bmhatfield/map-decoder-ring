# OneMap Explored Decoder

Go decoder for `One Map To Rule Them All` `.explored` files.

## Run

```sh
go run . fixtures/Dedicated.one_map_to_rule_them_all.explored
go run . --summary fixtures/Dedicated.one_map_to_rule_them_all.explored
go run . --pins fixtures/Dedicated.one_map_to_rule_them_all.explored
go run . --json decoded.json fixtures/Dedicated.one_map_to_rule_them_all.explored
go run . --pgm explored.pgm fixtures/Dedicated.one_map_to_rule_them_all.explored
```

## Test

```sh
go test ./...
```

## Duplicate Pins Observed

The raw fixture data contains many repeated pins with the same name, type, and exact position. That matches the mod's `ArePinsEqual(SharedPin, SharedPin)` identity check.

| File | Raw pins | Duplicate groups | Extra duplicate pins | Unique by name/type/position |
| --- | ---: | ---: | ---: | ---: |
| `fixtures/Dedicated.one_map_to_rule_them_all.explored` | 325 | 41 | 258 | 67 |
| `fixtures/Dedicated.one_map_to_rule_them_all.explored.old` | 39,255 | 1,404 | 33,824 | 5,431 |

Largest examples:

| File | Repeated pin | Count |
| --- | --- | ---: |
| current | `Tu 178` at `(806.4286, 39.3086, -811.5291)` | 27 |
| old | `Tu 386` at `(804.3380, 39.9261, -805.8654)` | 3,794 |

The decoder reports raw data as stored; it does not deduplicate pins.
