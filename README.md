# Mapperator

Mapperator is a library meant for efficient beatmap pattern search based on features like rhythm and distance.
This enables the conversion of features to real patterns to playable beatmaps.

Mapperator requires .NET 6 to run and only works for osu! standard gamemode beatmaps.

## Console App

Extract beatmap data from a collection in your osu! songs folder.
```
.\Mapperator.ConsoleApp.exe extract -c collection_name -o data
```

Extract beatmap data from a specific mapper in your osu! songs folder.
```
.\Mapperator.ConsoleApp.exe extract -s Ranked -m Standard -a Sotarks -o SotarksData
```

Reconstruct a beatmap using the extracted data
```
.\Mapperator.ConsoleApp.exe convert -d SotarksData -i path_to_beatmap.osu -o converted_beatmap
```

You can specify an osu! folder. There should be a `config.json` next to the executable with some paths. This is the contents:
```
{
  "OsuPath": "C:\\Users\\name\\AppData\\Local\\osu!",
  "SongsPath": "C:\\Users\\name\\AppData\\Local\\osu!\\Songs"
}
```

## Demo App

![](https://i.imgur.com/iU5TE28.png)

The left view shows the [original beatmap](https://osu.ppy.sh/beatmapsets/989342#osu/2069602), the right view shows the reconstruction in red. 

Clicking the **prev** or **next** buttons or clicking the timeline allows for seeking to different parts of the beatmap. Clicking the **variant** button generates the next best reconstruction of the pattern.

The Demo App uses the SotarksData dataset which contains all of Sotarks' ranked beatmaps and GD's.

