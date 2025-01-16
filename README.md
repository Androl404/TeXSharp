# TeXSharp

TeXSharp (TeX#) est un éditeur de LaTeX, écrit en C# avec la librairie GTK, écrit dans un objectif éducatif pour découvrir le langage C#.

![Logo de TeXSharp](https://raw.githubusercontent.com/Androl404/TeXSharp/refs/heads/main/assets/logo/logo_dark_fg.png)

# Compiler localement

Vous aurez besoin de Microsoft .NET installé sur votre machine, nous recommandons d'utiliser la dernière version (la version 9 actuellement). Plus d'informations ici : https://dotnet.microsoft.com/en-us/download.

```sh
$ git clone https://github.com/Androl404/TeXSharp.git
$ dotnet restore
$ dotnet build
$ dotnet run
```

Après avoir cloné le dépôt, la commande `dotnet run` suffit puisqu'elle restore les dépendances NuGet, compile le projet et le lance.
