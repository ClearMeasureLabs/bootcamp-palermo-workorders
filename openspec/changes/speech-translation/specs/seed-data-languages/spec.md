## ADDED Requirements

### Requirement: Seed data employees have culturally appropriate default languages
The `ZDataLoader` in `src/IntegrationTests/ZDataLoader.cs` SHALL set the `PreferredLanguage` property for seeded employees based on their cultural/ancestral background. Most employees SHALL default to `"en-US"`. Specific employees SHALL be assigned non-English preferred languages based on name-derived ancestry.

#### Scenario: Joe Cuevas has Spanish preferred language
- **WHEN** the `ZDataLoader.LoadData()` method creates the `jcuevas` employee
- **THEN** `jcuevas.PreferredLanguage` SHALL be set to `"es-ES"`

#### Scenario: Will Perea has Spanish preferred language
- **WHEN** the `ZDataLoader.LoadData()` method creates the `will` employee (Will Perea)
- **THEN** `will.PreferredLanguage` SHALL be set to `"es-ES"`

#### Scenario: Nick Larsen has German preferred language
- **WHEN** the `ZDataLoader.LoadData()` method creates the `nlarsen` employee
- **THEN** `nlarsen.PreferredLanguage` SHALL be set to `"de-DE"`

#### Scenario: Paige Ludecker has German preferred language
- **WHEN** the `ZDataLoader.LoadData()` method creates the `pludecker` employee (Paige Ludecker)
- **THEN** `pludecker.PreferredLanguage` SHALL be set to `"de-DE"`

#### Scenario: Apu Nahasapeemapetilon has default English preferred language
- **WHEN** the `ZDataLoader.LoadSimpsonsChurchData()` method creates the `apuNahasapeemapetilon` employee
- **THEN** `apuNahasapeemapetilon.PreferredLanguage` SHALL remain `"en-US"` (default)

#### Scenario: Groundskeeper Willie has default English preferred language
- **WHEN** the `ZDataLoader.LoadSimpsonsChurchData()` method creates the `groundskeeperWillie` employee
- **THEN** `groundskeeperWillie.PreferredLanguage` SHALL remain `"en-US"` (default, despite Scottish origin — the application uses BCP 47 codes for supported voices only)

#### Scenario: All other employees default to en-US
- **WHEN** `ZDataLoader.LoadData()` and `LoadSimpsonsChurchData()` create employees not listed above
- **THEN** their `PreferredLanguage` SHALL remain the default value of `"en-US"`

### Constraints
- Only set `PreferredLanguage` on employees where there is a reasonable cultural inference from the surname
- Supported language codes for this feature are: `"en-US"`, `"es-ES"`, `"de-DE"`, `"fr-FR"`
