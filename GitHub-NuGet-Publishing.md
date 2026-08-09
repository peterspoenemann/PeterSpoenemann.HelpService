# Öffentliche .NET-Bibliothek über GitHub und NuGet veröffentlichen

## Ziel

Eine selbst entwickelte .NET-Bibliothek soll:

- als öffentliches Open-Source-Projekt auf GitHub liegen,
- reproduzierbar gebaut und getestet werden,
- als NuGet-Paket (`.nupkg`) bereitgestellt werden,
- optional ein Symbolpaket (`.snupkg`) enthalten,
- über Git-Tags versioniert werden,
- und über GitHub Actions automatisch auf NuGet.org veröffentlicht werden.

Der empfohlene Ablauf ist:

```text
Sourcecode
   │
   ▼
GitHub
   │
   ├── Build
   ├── Tests
   └── Pack
         │
         ▼
      NuGet.org
         │
         ▼
dotnet add package <PackageId>
```

---

## 1. Repository-Struktur

Eine sinnvolle Struktur ist beispielsweise:

```text
MyLibrary/
│
├── src/
│   └── MyLibrary/
│       ├── MyLibrary.csproj
│       └── ...
│
├── tests/
│   └── MyLibrary.Tests/
│       ├── MyLibrary.Tests.csproj
│       └── ...
│
├── .github/
│   └── workflows/
│       └── publish.yml
│
├── README.md
├── CHANGELOG.md
├── LICENSE
└── MyLibrary.sln
```

Auf GitHub sollte möglichst der Quellcode liegen und nicht nur eine bereits kompilierte DLL.

---

## 2. NuGet-Metadaten in der `.csproj`

Für moderne SDK-Style-Projekte werden die Paketinformationen direkt in der Projektdatei gepflegt.

Beispiel:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>

    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <PackageId>MeineFirma.MyLibrary</PackageId>
    <Authors>Dein Name</Authors>
    <Description>Beschreibung der Bibliothek.</Description>
    <PackageTags>dotnet;library</PackageTags>

    <RepositoryUrl>https://github.com/USERNAME/MyLibrary</RepositoryUrl>
    <RepositoryType>git</RepositoryType>

    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>

    <PublishRepositoryUrl>true</PublishRepositoryUrl>

    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <ItemGroup>
    <None Include="../../README.md"
          Pack="true"
          PackagePath="\" />
  </ItemGroup>

</Project>
```

Eine separate `.nuspec`-Datei ist für übliche SDK-Style-Projekte normalerweise nicht erforderlich.

Die Version muss nicht zwingend dauerhaft in der `.csproj` stehen. Im unten beschriebenen GitHub-Workflow wird sie aus dem Git-Tag übernommen.

---

## 3. XML-Dokumentation

Öffentliche APIs sollten mit XML-Kommentaren dokumentiert werden:

```csharp
/// <summary>
/// Lädt ein Hilfethema.
/// </summary>
/// <param name="topic">Name des Hilfethemas.</param>
public void LoadTopic(string topic)
{
    // ...
}
```

Mit

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

wird neben der Assembly eine XML-Dokumentationsdatei erzeugt. IDEs wie Visual Studio können diese Informationen für IntelliSense verwenden.

---

## 4. Lizenz

Für ein öffentlich verwendbares Open-Source-Projekt sollte eine explizite Lizenz gewählt werden.

Eine häufig verwendete, sehr permissive Variante ist MIT.

Im Repository:

```text
LICENSE
```

In der `.csproj`:

```xml
<PackageLicenseExpression>MIT</PackageLicenseExpression>
```

Die Lizenz sollte bewusst passend zum gewünschten Nutzungsmodell gewählt werden.

---

## 5. Paket lokal erzeugen und prüfen

Vor der Automatisierung sollte der Paket-Build lokal funktionieren.

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release
```

Typische Ausgabedateien:

```text
MyLibrary.1.0.0.nupkg
MyLibrary.1.0.0.snupkg
```

Die `.nupkg` enthält das eigentliche Paket.  
Die `.snupkg` enthält Symbole für Debugging und Source-Navigation.

Vor der ersten Veröffentlichung sollte geprüft werden:

- Ist die richtige DLL enthalten?
- Ist die XML-Dokumentation enthalten?
- Wird die README angezeigt?
- Stimmen Paket-ID, Beschreibung und Lizenz?
- Sind nur notwendige Dateien enthalten?
- Sind alle Paketabhängigkeiten korrekt?

---

## 6. GitHub-Repository veröffentlichen

Ein neues öffentliches Repository auf GitHub anlegen, beispielsweise:

```text
https://github.com/USERNAME/MyLibrary
```

Danach lokal:

```bash
git init
git add .
git commit -m "Initial version"

git branch -M main
git remote add origin https://github.com/USERNAME/MyLibrary.git
git push -u origin main
```

---

## 7. NuGet.org

Auf NuGet.org wird ein Benutzerkonto benötigt.

Die `PackageId` muss eindeutig sein, beispielsweise:

```xml
<PackageId>MeineFirma.MyLibrary</PackageId>
```

Nach der Veröffentlichung kann das Paket installiert werden mit:

```bash
dotnet add package MeineFirma.MyLibrary
```

---

## 8. Semantic Versioning

Für öffentliche Pakete empfiehlt sich:

```text
MAJOR.MINOR.PATCH
```

Beispiele:

```text
1.0.0       erste stabile Version
1.0.1       Bugfix
1.1.0       neue kompatible Funktion
2.0.0       Breaking Change
```

Vorabversionen:

```text
1.1.0-alpha.1
1.1.0-beta.1
1.1.0-rc.1
```

---

## 9. Releases über Git-Tags

Pakete sollten nicht bei jedem Push auf `main` veröffentlicht werden.

Stattdessen wird ein Release durch einen Tag ausgelöst:

```bash
git tag v1.0.0
git push origin v1.0.0
```

Der GitHub-Workflow entfernt das führende `v` und verwendet:

```text
1.0.0
```

als NuGet-Paketversion.

Damit gilt:

```text
Git-Tag       NuGet-Version

v1.0.0   →    1.0.0
v1.0.1   →    1.0.1
v1.1.0   →    1.1.0
v2.0.0   →    2.0.0
```

---

## 10. NuGet Trusted Publishing

Für neue Projekte ist NuGet Trusted Publishing eine gute Wahl.

Dabei wird kein dauerhaft gültiger NuGet-API-Key als GitHub-Secret gespeichert. GitHub Actions fordert über OpenID Connect (OIDC) ein kurzlebiges Token an. NuGet.org prüft dieses Token anhand einer zuvor eingerichteten Trusted-Publishing-Policy und stellt anschließend einen kurzlebigen API-Key zum Publizieren bereit.

Der GitHub-Job benötigt dafür:

```yaml
permissions:
  contents: read
  id-token: write
```

`id-token: write` erlaubt dem Workflow, ein OIDC-Token anzufordern.

---

## 11. Trusted Publishing auf NuGet.org einrichten

Auf NuGet.org:

1. Anmelden.
2. Den Bereich **Trusted Publishing** öffnen.
3. Eine neue Trusted-Publishing-Policy für GitHub anlegen.
4. Repository Owner eintragen.
5. Repository-Namen eintragen.
6. Als Workflow-Datei `publish.yml` angeben.

Wichtig: Bei der Workflow-Datei wird nur der Dateiname angegeben:

```text
publish.yml
```

nicht:

```text
.github/workflows/publish.yml
```

Optional kann zusätzlich ein GitHub Environment wie `release` verwendet werden.

---

## 12. NuGet-Benutzername als GitHub Secret

Der offizielle `NuGet/login`-Action benötigt den NuGet.org-Profilnamen.

Im GitHub-Repository unter:

```text
Settings
→ Secrets and variables
→ Actions
```

ein Secret anlegen:

```text
NUGET_USER
```

Als Wert wird der NuGet.org-Benutzername bzw. Profilname verwendet, **nicht die E-Mail-Adresse**.

Dieses Secret ist kein API-Key.

---

## 13. GitHub Actions Workflow

Die Datei wird gespeichert als:

```text
.github/workflows/publish.yml
```

Der mit dieser Anleitung gelieferte Beispiel-Workflow:

- startet bei Tags `v*`,
- baut das Projekt,
- führt Tests aus,
- übernimmt die Version aus dem Git-Tag,
- erzeugt `.nupkg` und `.snupkg`,
- authentifiziert sich per OIDC bei NuGet.org,
- und veröffentlicht das Paket.

Vor Verwendung müssen mindestens angepasst werden:

```yaml
env:
  PROJECT_FILE: src/MyLibrary/MyLibrary.csproj
  DOTNET_VERSION: 8.0.x
```

`PROJECT_FILE` muss auf die eigene `.csproj` zeigen.

`DOTNET_VERSION` sollte zum verwendeten SDK passen.

---

## 14. Ersten Release veröffentlichen

Wenn alles committed und nach GitHub gepusht wurde:

```bash
git tag v1.0.0
git push origin v1.0.0
```

Daraufhin startet automatisch:

```text
GitHub Actions
    │
    ├── Restore
    ├── Build
    ├── Test
    ├── Pack
    ├── NuGet OIDC Login
    └── Publish
          │
          ▼
       NuGet.org
```

Der Status kann im GitHub-Repository unter **Actions** kontrolliert werden.

---

## 15. Typischer Entwicklungsablauf

Normale Entwicklung:

```text
Code ändern
   │
   ▼
Tests
   │
   ▼
Commit
   │
   ▼
Push auf main
```

Neues Release:

```text
Änderungen fertigstellen
   │
   ▼
Tests erfolgreich
   │
   ▼
CHANGELOG aktualisieren
   │
   ▼
Commit + Push
   │
   ▼
Git-Tag vX.Y.Z
   │
   ▼
Tag pushen
   │
   ▼
GitHub Actions
   │
   ▼
NuGet.org
```

---

## 16. Checkliste vor der ersten Veröffentlichung

- [ ] Öffentliches GitHub-Repository angelegt
- [ ] Sourcecode eingecheckt
- [ ] `.gitignore` vorhanden
- [ ] `README.md` vorhanden
- [ ] `LICENSE` vorhanden
- [ ] NuGet `PackageId` festgelegt
- [ ] Package-Metadaten in der `.csproj` gepflegt
- [ ] XML-Dokumentation aktiviert
- [ ] Symbolpaket aktiviert
- [ ] `dotnet build -c Release` erfolgreich
- [ ] Tests erfolgreich
- [ ] `dotnet pack -c Release` erfolgreich
- [ ] erzeugtes `.nupkg` geprüft
- [ ] NuGet.org-Konto vorhanden
- [ ] Trusted-Publishing-Policy auf NuGet.org angelegt
- [ ] Workflow-Datei dort als `publish.yml` eingetragen
- [ ] GitHub Secret `NUGET_USER` angelegt
- [ ] `PROJECT_FILE` in `publish.yml` angepasst
- [ ] verwendete .NET-SDK-Version angepasst
- [ ] erster Tag `v1.0.0` gepusht
- [ ] GitHub Action erfolgreich durchgelaufen
- [ ] Paket auf NuGet.org sichtbar

---

## 17. Referenzen

Offizielle Dokumentation:

- NuGet Trusted Publishing:  
  https://learn.microsoft.com/nuget/nuget-org/trusted-publishing

- GitHub Actions OpenID Connect:  
  https://docs.github.com/actions/concepts/security/openid-connect

- `dotnet nuget push`:  
  https://learn.microsoft.com/dotnet/core/tools/dotnet-nuget-push

- NuGet Symbol Packages (`.snupkg`):  
  https://learn.microsoft.com/nuget/create-packages/symbol-packages-snupkg
