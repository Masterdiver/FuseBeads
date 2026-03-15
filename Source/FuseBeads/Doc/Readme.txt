Apk in Package Manager Console bauen:
dotnet publish -f:net10.0-android -c Release

Um aus Visual Studio ein Android-Installationspaket (APK) zu erstellen, folge diesen Schritten:

1. Projekt öffnen
Öffne dein Xamarin.Android-, .NET MAUI- oder Android-Projekt in Visual Studio.

2. Build-Konfiguration einstellen
Stelle sicher, dass du die Konfiguration Release (nicht Debug) oben im Dropdown auswählst.
Wähle als Zielgerät „Android“ aus.
3. APK erstellen
Für Xamarin.Android-Projekte:
Rechtsklick auf das Android-Projekt im Projektmappen-Explorer, dann „Archivieren...“ wählen (erscheint, wenn du Xamarin-Android verwendest).
Im nun erscheinenden Archivierungs-Manager kannst du dein APK signieren und exportieren.
Möchtest du nur das unsigned APK: Menü Build > Build Solution. Die APK findest du anschließend im Pfad:
bin\Release deines Android-Projekts.
Alternativ:

Menü Build > Projektmappe bereitstellen oder Projektmappe erstellen
-> Der Build-Vorgang erstellt das APK (und ggf. auch die .aab – Android App Bundle).
Für .NET MAUI-Projekte:
Öffne ein Terminal (bzw. „Paket-Manager-Konsole“ in Visual Studio).
Führe folgenden Befehl aus:
Shell
dotnet publish -f:net8.0-android -c Release
Das APK findest du nach erfolgreichem Build in: bin\Release\net8.0-android\publish\
4. APK signieren
Für Publikationen im Google Play Store musst du das APK signieren (über den Archivierungs-Manager oder eigene Keystore-Datei).
Für Testzwecke genügt meistens das automatisch generierte Debug-APK.
5. APK auf Gerät übertragen
Das fertige APK kannst du auf dein Android-Gerät kopieren und installieren.