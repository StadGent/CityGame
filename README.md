# CityGame

3D City Game for Ghent

## Installatie

Download en installeer Unity versie 5.5.0: https://unity3d.com/get-unity/download/archive  return
Download en installeer git: https://git-scm.com/downloads  return
Download en installeer SourceTree: https://www.sourcetreeapp.com/  return
Tijdens installatie:  return
Als er wordt gevraagd naar Mercurial: selecteer dan om de embedded versie te installeren.

## Setup

Start SourceTree en maak een nieuwe repository aan.
Selecteer het tabblad om een repository te "clonen" en vul volgende gegevens in:
- Source Path/URL: https://github.com/StadGent/CityGame.git
- Destination Path: Browse naar de map waar je Unity projecten staan, standaard is dit "Documenten" of "Documenten\Unity", en vul dit aan met "\CityGame"
Klik op "Clone" om de repository binnen te halen.  return
Na het downloaden:  return
Ga in het menu naar "Repository->Git LFS->Initialise Repository".
Als er wordt gevraag om Git LFS te installeren, druk dan op "OK".
Voeg volgende extenties toe aan de Git LFS track lijst: dae, fbx, jpeg, jpg, png, psd, tga, wav (Deze lijst kan nog worden uitgebreid)
Als je geen Git LFS track venster te zien krijgt kan je die openen via het menu: Repository->Git LFS->Track/untrack Files
Ga in het menu naar "Repository->Git LFS->Pull LFS Content" om de grotere bestanden te downloaden.
Als je in het menu na het initialiseren van Git LFS nog niet "Pull LFS Content" kan kiezen dan moet je even wachten totdat SourceTree gedaan heeft met de initialisatie.
Als alles is gedownload mag je SourceTree sluiten.

Start Unity, selecteer om een project te openen en browse naar de locatie waar je de repository hebt gedownload.
Als Unity alles heeft geïmporteerd sleep dan volgende scenes in het Hierarchy venster:
- Core in de root.
- Gent LOD_2 en Veerleplein in de _Common\Scenes map.
Verwijder de Untitled scene en druk op Play.

*English Version*
## Installation

Download and install Unity version 5.5.0: https://unity3d.com/get-unity/download/archive  return
Download and install git: https://git-scm.com/downloads  return
Download en install SourceTree: https://www.sourcetreeapp.com/  return
During installation:  return
When asked for Mercurial: select to install the embedded version.

## Setup

Start SourceTree and create a new repository.
Select the tab to clone a repository and fill in following information:
- Source Path/URL: https://github.com/StadGent/CityGame.git
- Destination Path: <Browse to the map where your Unity projects are located, standaard  this is "Documents" or "Documents\Unity", and append this with "\CityGame">
Click "Clone" to download the repository.  return
After download:  return
In the menu go to "Repository->Git LFS->Initialise Repository"
When asked to install Git LFS, press "OK".
Add the following extentions to the Git LFS track list: dae, fbx, jpeg, jpg, png, psd, tga, wav (This list may get extended)
If you don't see a Git LFS track list, you can open this through the menu: Repository->Git LFS->Track/untrack Files
In the menu go to "Repository->Git LFS->Pull LFS Content" to download the larger files.
When after initialising Git LFS you can't choose "Pull LFS Content" you'll have to wait until SourceTree is done with the initialisation.
After everything is downloaded you may close SourceTree.

Start Unity, select to open a new project and browse to the location where you downloaded the repository.
When Unity has finished importing the assets drag following scenes in the Hierarchy window:
- Core in the root.
- Gent LOD_2 and Veervleplein in the _Common\Scenes map.
Remove the Untitled scene and press Play.