# Mapperator

Mapperator is a library meant for efficient beatmap pattern search based on features like rhythm and distance.
This enables the conversion of features to real patterns to playable beatmaps.

Mapperator requires .NET 8 to run and only works for osu! standard gamemode beatmaps.

## Console App

Extract beatmap data from a collection in your osu! songs folder.
```
.\Mapperator.ConsoleApp.exe extract -c collection_name -o data
```

Extract beatmap data from a specific mapper in your osu! songs folder.
```
.\Mapperator.ConsoleApp.exe extract -s Ranked -m Standard -a Sotarks -o SotarksData
```

Reconstruct a beatmap using the extracted data.
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

Create a generic dataset for Machine Learning from your songs folder.

```
.\Mapperator.ConsoleApp.exe dataset -s Ranked -m Standard -i 200000 -o "path to output folder"
```

### Create a high-quality dataset

With the verb `dataset2` you can create a dataset in the new format with rich metadata from the osu! API.
It requires an [osu! OAuth client token](https://osu.ppy.sh/home/account/edit) to be present in the `config.json` file that is automatically generated in the same folder when you run the console app executable.

The input data is given by a path to an input folder. It will search the input folder recursively for any .osz files or .zip archives containing .osz files.
The input beatmaps must all be uploaded to the osu! website, otherwise there is no metadata to be found.

Repeat runs with the same output folder will generally attempt to append to the existing dataset, skipping over any beatmaps that are already in the dataset. If you instead wish to replace entries with your new input data, you can use the 'override' arguments to specify any categories that you would like to replace.

```bash
Mapperator.ConsoleApp.exe dataset2 -i "path/to/osz/files" -o "/datasets/cool_dataset"
```

<details>
<summary>Dataset file structure</summary>
  
```
OutputFolder
├── metadata.parquet
├── data
│   ├── 1 Kenji Ninuma - DISCO PRINCE
│   │   ├── 20.mp3
│   │   ├── Kenji Ninuma - DISCOPRINCE (peppy) [Normal].osu
│   ├── 3 Ni-Ni - 1,2,3,4, 007 [Wipeout Series]
│   │   ├── 1,2,3,4, 007 (Speed Pop Mix).mp3
│   │   ├── Ni-Ni - 1,2,3,4, 007 [Wipeout Series] (MCXD) [-Breezin-].osu
│   │   ├── Ni-Ni - 1,2,3,4, 007 [Wipeout Series] (MCXD) [-Crusin-].osu
│   │   ├── Ni-Ni - 1,2,3,4, 007 [Wipeout Series] (MCXD) [-Hardrock-].osu
│   │   ├── Ni-Ni - 1,2,3,4, 007 [Wipeout Series] (MCXD) [-Sweatin-].osu
...
```
</details>

<details>

<summary>Included metadata</summary>

```
>>> df.loc[989342].iloc[0]
Artist                                     Denkishiki Karen Ongaku Shuudan
ArtistUnicode                                                    電気式華憐音楽集団
Creator                                                           OliBomby
FavouriteCount                                                          87
Nsfw                                                                 False
Offset                                                                   0
BeatmapSetPlayCount                                                 100314
Source                                                                    
BeatmapSetStatus                                                    ranked
Spotlight                                                            False
Title                                                 Aoki Kotou no Anguis
TitleUnicode                                                   碧き孤島のアングゥィス
BeatmapSetUserId                                                   6573093
Video                                                                False
Description              <div class='bbcode bbcode--normal-line-height'...
GenreId                                                                 11
GenreName                                                            Metal
LanguageId                                                               3
LanguageName                                                      Japanese
PackTags                                                            [S862]
Ratings                               [0, 4, 1, 0, 0, 1, 1, 3, 2, 13, 116]
DownloadDisabled                                                     False
BeatmapSetBpm                                                        197.0
CanBeHyped                                                           False
DiscussionLocked                                                     False
BeatmapSetIsScoreable                                                 True
BeatmapSetLastUpdated                                  2020-01-28 13:23:12
BeatmapSetRanked                                                         1
RankedDate                                             2020-02-06 13:02:45
Storyboard                                                           False
SubmittedDate                                          2019-06-18 12:02:28
Tags                     denkare metal power gothic japanese carnaval t...
DifficultyRating                                                      6.16
Mode                                                                   osu
Status                                                              ranked
TotalLength                                                            253
UserId                                                             6573093
Version                                                        Ardens Spes
Checksum                                  f05ed490aece35b410421d7009dfb238
MaxCombo                                                              1779
Accuracy                                                               9.0
Ar                                                                     9.3
Bpm                                                                  197.0
CountCircles                                                          1014
CountSliders                                                           362
CountSpinners                                                            2
Cs                                                                     4.0
Drain                                                                  6.0
HitLength                                                              253
IsScoreable                                                           True
LastUpdated                                            2020-01-28 13:23:12
ModeInt                                                                  0
PassCount                                                             4653
PlayCount                                                            51390
Ranked                                                                   1
StarRating               [3.5099857, 4.8706975, 6.1123815, 7.53119, 9.0...
OmdbTags                 [simple, geometric, bursts, stamina, clean, ma...
AudioFile                                                        audio.mp3
BeatmapSetFolder         989342 Denkishiki Karen Ongaku Shuudan - Aoki ...
BeatmapFile              Denkishiki Karen Ongaku Shuudan - Aoki Kotou n...
Name: 2069601, dtype: object
```

</details>

## Demo App

![](https://i.imgur.com/iU5TE28.png)

The left view shows the [original beatmap](https://osu.ppy.sh/beatmapsets/989342#osu/2069602), the right view shows the reconstruction in red. 

Clicking the **prev** or **next** buttons or clicking the timeline allows for seeking to different parts of the beatmap. Clicking the **variant** button generates the next best reconstruction of the pattern.

The Demo App uses the SotarksData dataset which contains all of Sotarks' ranked beatmaps and GD's.

