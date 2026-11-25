# Third-party test data

Known sources of the ID numbers in `Civitas.Id.Sweden.Tests/Fixtures/`.

## Skatteverket

The numbers in the six files listed first below come from Skatteverket's published test data sets. Skatteverket
states that they are for use in test environments only. Both data sets are licensed CC0 1.0 in their metadata:

- Testpersonnummer: <https://skatteverket.entryscape.net/store/9/metadata/147?recursive=dcat>
- Testsamordningsnummer: <https://skatteverket.entryscape.net/store/9/metadata/153?recursive=dcat>

Each file below is pinned by hash and row count in `Civitas.Id.Sweden.Tests/Helpers/FixtureSpec.cs`.
`.gitattributes` marks `Civitas.Id.Sweden.Tests/Fixtures/**` as `-text`, so Git never rewrites their line endings.
Retrieval and comparison below were done on 2026-11-24, and on 2026-11-25 for the two files of 2000-2009 and 2020.

## Testsamordningsnummer_2019_official.csv

- File: `Civitas.Id.Sweden.Tests/Fixtures/Testsamordningsnummer_2019_official.csv`
- Source: Skatteverket, "Testsamordningsnummer 2019",
  <https://skatteverket.entryscape.net/store/9/resource/2456>
- Retrieved byte for byte: 20,995 bytes, ASCII, CRLF line endings, one header line and 1498 numbers
- SHA-256: `54e3fe8b4bc9d6f51b67139266fb284f251af0003b9e3a152873c6ca570af838`

## Testsamordningsnummer_2020_official.csv

- File: `Civitas.Id.Sweden.Tests/Fixtures/Testsamordningsnummer_2020_official.csv`
- Source: Skatteverket, "Testsamordningsnummer 2020",
  <https://skatteverket.entryscape.net/store/9/resource/2458>
- Retrieved byte for byte: 219 bytes, ASCII, CRLF line endings, one header line and 14 numbers
- SHA-256: `4e22aef1f54a2e59f168558281b883f82c9eecd8258bde6efd1126677ac0e288`

## Testpersonnummer_2000-2009_official.csv

- File: `Civitas.Id.Sweden.Tests/Fixtures/Testpersonnummer_2000-2009_official.csv`
- Source: Skatteverket, "Testpersonnummer 2000-2009",
  <https://skatteverket.entryscape.net/store/9/resource/1939>
- Retrieved byte for byte: 102,540 bytes, ASCII, CRLF line endings, one header line and 7323 numbers
- SHA-256: `de273ddbbbbc5c13ff5017230f6d94a65f3df3f70d04b26de0769cf6f8daa50f`

## Testpersonnummer 1890-1899.csv

- File: `Civitas.Id.Sweden.Tests/Fixtures/Testpersonnummer 1890-1899.csv`
- Source: Skatteverket, "Testpersonnummer 1890-1899",
  <https://skatteverket.entryscape.net/store/9/resource/149>
- Its 3653 numbers equal those of the source file, in the same order. The source has a header line and CRLF
  line endings; this file has no header line and LF line endings.

## testpersonnummer_final.csv

- File: `Civitas.Id.Sweden.Tests/Fixtures/testpersonnummer_final.csv`
- Its 3653 numbers (first column) equal those of resource 149 above, in the same order. The file adds the
  columns `Female`, `Male`, `Adult`, `Child`, `Age` and `Valid`.

## Testsamordningsnummer_2019_corrected.csv

- File: `Civitas.Id.Sweden.Tests/Fixtures/Testsamordningsnummer_2019_corrected.csv`
- Its 1306 numbers (first column) are 1306 of the 1498 numbers in the official 2019 file above, in the same
  order. The file adds the columns `Female`, `Male`, `Age`, `Adult`, `Child` and `Valid`.

## testorganisationsnummer_enskild_firma.csv

- File: `Civitas.Id.Sweden.Tests/Fixtures/testorganisationsnummer_enskild_firma.csv`
- Its 16 numbers are numbers of the official files above: 3 from the 1890-1899 file, 3 from the 2000-2009 file and 10
  from the 2019 samordningsnummer file. Every column is those digits rewritten: with or without century, separator,
  `SE` prefix and VAT suffix.

## testorganisationsnummer_extended.csv

- File: `Civitas.Id.Sweden.Tests/Fixtures/testorganisationsnummer_extended.csv`
- Source: organisationsnummer/meta, `testdata/list.json`, <https://github.com/organisationsnummer/meta>. All 10 input
  values occur there.
- Licence: MIT

```text
MIT License

Copyright (c) Organisationsnummer and Contributors

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
```

## Files of unknown origin

- `testorganisationsnummer.txt` (6 rows): origin not documented; none of its rows is in organisationsnummer/meta.
- `invalid_swedish_ids.csv` (9 rows): origin not documented; none of its rows is in organisationsnummer/meta.
