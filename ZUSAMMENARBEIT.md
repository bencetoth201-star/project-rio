# Zu zweit an PROJECT RIO arbeiten

Kurzfassung: **GitHub**. Das Projekt liegt dort als privates Repository, ihr
habt beide eine eigene Kopie auf eurem Rechner, und Git gleicht die
Änderungen ab. Jeder lässt Claude Code in seiner eigenen Kopie laufen.

---

## Einmalig: Repository anlegen (einer von euch)

Das Projekt ist lokal schon ein Git-Repository mit 16 Commits. Es fehlt nur
der Platz im Internet, an den es geschoben wird.

1. Auf **github.com** einloggen (oder kostenlos registrieren).
2. Oben rechts **+ → New repository**.
   - Name: `project-rio`
   - **Private** auswählen
   - **Nichts** ankreuzen — kein README, kein .gitignore, keine Lizenz.
     Das Repository muss leer sein, sonst gibt es beim ersten Push Ärger.
3. **Create repository**.
4. Dann im Terminal, im Projektordner:

```powershell
git remote add origin https://github.com/DEIN-NAME/project-rio.git
git push -u origin main
```

GitHub fragt nach Login. Das läuft über den Browser oder über ein
Personal Access Token — am einfachsten ist, vorher **GitHub Desktop** oder
den **Git Credential Manager** installiert zu haben, dann öffnet sich
einfach ein Fenster.

5. **Freund einladen:** im Repository auf **Settings → Collaborators →
   Add people**, seinen GitHub-Namen eintragen. Er bekommt eine Mail.

---

## Einmalig: beim Freund

Er braucht dasselbe wie du:

- **Unity 6000.6.10f1** — exakt diese Version, sonst schreibt Unity bei ihm
  alle Projektdateien um und ihr habt bei jedem Commit Krach
- **Git** und **Git LFS**
- **Claude Code**

Dann:

```powershell
git lfs install
git clone https://github.com/DEIN-NAME/project-rio.git "C:\dev\PROJECT RIO"
```

**Wichtig:** nicht in OneDrive klonen. OneDrive synchronisiert Unitys
`Library`-Ordner mit über 20.000 Dateien und macht das Projekt unbenutzbar.
`C:\dev\...` oder irgendwas anderes außerhalb von OneDrive.

Danach Unity öffnen und einmal:

1. **Tools → Rio → Welt bauen (20 km2)**
2. **Tools → Rio → Block-Figur bauen**

Vorher ist die Szene leer — das ist richtig so, siehe unten.

---

## Der tägliche Ablauf

**Vor dem Anfangen immer:**

```powershell
git pull
```

**Für jede Aufgabe einen eigenen Zweig:**

```powershell
git switch -c strassen-tunnel
```

Dann arbeiten (auch mit Claude Code), und wenn es läuft:

```powershell
git add -A
git commit -m "Tunnel durch die Morros"
git push -u origin strassen-tunnel
```

GitHub zeigt dann einen Knopf **Compare & pull request**. Draufklicken, der
andere schaut drüber, **Merge**. Danach bei beiden:

```powershell
git switch main
git pull
```

In Claude Code geht das auch mit `/ship` — das committet, pusht, macht den
Pull Request auf und begleitet ihn.

---

## Die vier Regeln, die bei Unity wirklich zählen

**1. Nie beide gleichzeitig an derselben Datei.**
Das ist die wichtigste. C#-Dateien kann Git zusammenführen, solange ihr
verschiedene Stellen anfasst — aber `RioStrassenplan.cs` hat 1400 Zeilen,
und wenn ihr beide darin arbeitet, wird es hässlich. Sagt euch vorher, wer
woran sitzt.

**2. Die Szene liegt nicht im Repo.**
`SampleScene.unity` ist das *Ergebnis* der Generatoren, nicht ihre Quelle.
Nach einem Weltbau stehen dort über 1000 Objekte und 2,3 MB, die sich bei
jedem Lauf komplett ändern. Zwei Leute, die beide bauen, bekommen garantiert
einen Konflikt, den Git nicht auflösen kann. Deshalb ist sie in
`.gitignore`. Jeder baut sie lokal — bei festem Seed kommt bei beiden
dasselbe heraus.

Dasselbe gilt für `Assets/_Project/Generated/` (47 MB Meshes).

**3. Immer `git pull` vor dem Anfangen, immer pushen vor dem Aufhören.**
Ein Zweig, der drei Tage lokal liegt, lässt sich kaum noch einfügen.

**4. Die großen Dateien laufen über Git LFS.**
Ist schon eingerichtet (`.gitattributes`), alle `.glb` und Bilder gehen
darüber. GitHub gibt dafür kostenlos 1 GB Speicher und 1 GB Download pro
Monat. Ihr liegt bei **21 MB**, also weit darunter — aber ladet keine
Videos oder unkomprimierten Texturen hoch, das ist schnell aufgebraucht.

---

## Vorschlag zur Arbeitsteilung

Das Projekt ist gerade ein großer Weltgenerator. Wenn ihr beide daran
arbeitet, stoßt ihr dauernd zusammen. Besser nach Gebieten trennen:

| Gebiet | Dateien |
|---|---|
| **Welt und Straßen** | `RioStrassenplan.cs`, `RioStrassennetz.cs`, `RioWorldTerrain.cs`, `RioDistricts.cs` |
| **Häuser und Viertel** | `RioHousePlanner.cs`, `RioHouseBaker.cs` |
| **Spiel** | Steuerung, Fahrzeuge, HUD, Missionen — gibt es noch fast nicht, viel Platz |
| **Darstellung** | Streamer, LOD, GPU-Instancing, Materialien |

Der naheliegende Schnitt: einer macht die Welt fertig (Schnellstraßen,
Stützmauern, Favela-Wege — steht alles in `UEBERGABE.md`), der andere fängt
mit dem Spiel an: Auto fahren, Kamera, HUD. Die beiden berühren sich kaum.

---

## Wenn doch mal ein Konflikt kommt

Nicht in Panik raten. `git status` sagt, welche Datei betroffen ist, und
Claude Code kann den Konflikt lesen und auflösen — zeig ihm einfach die
Datei. Was du **nie** tun solltest: `git push --force` auf `main`. Damit
löschst du die Arbeit des anderen.
