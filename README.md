# TeXSharp

TeXSharp (TeX#) est un éditeur de LaTeX, écrit en C# avec la librairie GTK, conçu dans un objectif éducatif pour découvrir le langage C# et le monde de la programation événementielle.

> [!WARNING]
> TeXSharp est en cours de développement et il peut être instable. À utiliser à vos risques et périls.

![Logo de TeXSharp](https://raw.githubusercontent.com/Androl404/TeXSharp/refs/heads/main/assets/logo/logo_dark_fg_stoke.png)

# Dépendances

TeXsharp utilise la librairie GTK4 pour son interface graphique, ainsi que [Git.core](https://gircore.github.io/) pour permettre le lien entre le langage et la librairie. GTK4 doit être installé sur votre système.

Bien évidemment, il faut que le SDK .NET et le runtime .NET soit installés sur la machine. Nous vous recommandons d'utiliser la dernière version de .NET (la version 9 actuellement). Plus d'informations sur le téléchargement de .NET ici : [https://dotnet.microsoft.com/en-us/download](https://dotnet.microsoft.com/en-us/download).

## GNU/Linux

Sur Debian/Ubuntu, il faut installer la librairie GTK4 ainsi que les fichiers de développement :
```sh
$ sudo apt install libgtk-4-dev
```

Sur les autres distributions, référez vous à votre gestionnaire de paquets pour installer cette librairie. Le nom de la librairie peut potentiellement changer selon les distributions.

## MS Windows

Pour Windows, nous vous recommandons d'installer `msys2` ([site officiel](https://www.msys2.org/)) et ensuite d'installer les paquets dont on va avoir besoin ainsi que toutes les dépendances :

```sh
$ pacman -Suy
$ pacman -S mingw-w64-x86_64-gtk4
```

Il existe sûrement d'autres moyens d'installer ces paquets, la méthode que nous proposons ici est la plus simple. Ajoutez ensuite `C:\msys64\mingw64\bin` dans le PATH en tant que premier élément.

Nous vous conseillons d'utiliser l'interface .NET en ligne de commande pour travailler sur ce projet. Si vous souhaitez utiliser Visual Studio, nous ne documenterons pas cela ici. De plus, nous vous laisserons créer votre propre fichier de solution Visual.

# Compiler localement

Commencez par cloner le dépôt :

```sh
$ git clone https://github.com/Androl404/TeXSharp.git
```

Après avoir installé toutes les dépendances de ce projet :

```sh
$ dotnet restore   # Facultatif
$ dotnet build     # Facultatif
$ dotnet run
```

La commande `dotnet run` suffit puisqu'elle restaure les dépendances NuGet, compile le projet et lance TeXSharp.

> [!NOTE]
> Lorsque ce projet aura assez mûri, nous mettrons à disposition des fichiers binaires à exécuter. Pour l'instant, il faut compiler TeXSharp depuis la source.
