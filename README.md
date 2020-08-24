# Pixiv UWP 2 [![编译状态](https://dev.azure.com/tobiichiamane/pixivfs-uwp/_apis/build/status/tobiichiamane.pixivfs-uwp?branchName=master)](https://dev.azure.com/tobiichiamane/pixivfs-uwp/_build?definitionId=1)

[<img src='https://assets.windowsphone.com/85864462-9c82-451e-9355-a3d5f874397a/English_get-it-from-MS_InvariantCulture_Default.png' alt='English badge' width=284 height=104/>](https://www.microsoft.com/store/apps/9PM8K64J71PL?cid=storebadge&ocid=badge)

已发布至商店。所有的依赖都是Nuget包，所以除了安装有[Multilingual App Toolkit](http://aka.ms/matinstall)的VS2019和Windows SDK 17763以外你不需要任何特殊的环境来编译代码。你也可以从Azure Pipelines获取Artifact，或者从Release页面获取包。

Almost useable. All dependencies are available on Nuget, so you don't need any special environment to compile except Visual Studio 2019 with [Multilingual App Toolkit](http://aka.ms/matinstall) installed and Windows SDK 17763. Instead, you can get the compiled artifact from Azure Pipelines, or get it from the releases page.

由于.NET Native[暂不完整支持F#](https://github.com/dotnet/corert/issues/6055)，本应用暂时使用[pixivcs](https://github.com/tobiichiamane/pixivcs)而不是[pixivfs](https://github.com/tobiichiamane/pixivfs)编写。

Because .NET Native [hasn't fully supported F# yet](https://github.com/dotnet/corert/issues/6055), this project is based on [pixivcs](https://github.com/tobiichiamane/pixivcs) rather than [pixivfs](https://github.com/tobiichiamane/pixivfs). 

如果您也使用安卓设备，那么[@Notsfsssf](https://github.com/Notsfsssf)开发的[Pix-EzViewer](https://github.com/Notsfsssf/Pix-EzViewer)会是不错的选择。

If you also use Android devices, [Pix-EzViewer](https://github.com/Notsfsssf/Pix-EzViewer) developed by [@Notsfsssf](https://github.com/Notsfsssf) will be a great alternative to the official app.

如果您正使用版本低于Windows 10 Version 17134或更旧的操作系统（包括Windows 8.1、Windows 8和Windows 7在内），或者单纯希望获得非UWP的桌面体验，则[@Rinacm](https://github.com/Rinacm)开发的[Pixeval](https://github.com/Rinacm/Pixeval)将会给予您优秀的使用体验。

If you are running Windows 10 version 17134 or older (including Windows 8, Windows 8.1 and Windows 7), or you just want a non-UWP desktop experience, [Pixeval](https://github.com/Rinacm/Pixeval) developed by [@Rinacm](https://github.com/Rinacm) will be the perfect choice.

# 翻译/Translation

本应用急需翻译人员。参照[如何翻译](https://github.com/tobiichiamane/pixivfs-uwp/blob/master/Translate.md)。

Any help to translate this app will be appreciated. Please read [*How to translate*](https://github.com/tobiichiamane/pixivfs-uwp/blob/master/Translate.md).

# 参与者/Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tr>
    <td align="center"><a href="https://www.chenxublog.com/"><img src="https://avatars3.githubusercontent.com/u/10357394?v=4" width="100px;" alt=""/><br /><sub><b>chenxuuu</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/commits?author=chenxuuu" title="Documentation">📖</a></td>
    <td align="center"><a href="https://qaq.jp"><img src="https://avatars1.githubusercontent.com/u/24848528?v=4" width="100px;" alt=""/><br /><sub><b>逢坂桜</b></sub></a><br /><a href="#translation-SakuraSa233" title="Translation">🌍</a></td>
    <td align="center"><a href="https://github.com/TuMIer"><img src="https://avatars2.githubusercontent.com/u/45781074?v=4" width="100px;" alt=""/><br /><sub><b>TuMIer</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/issues?q=author%3ATuMIer" title="Bug reports">🐛</a></td>
    <td align="center"><a href="http://razesoldier.cn"><img src="https://avatars0.githubusercontent.com/u/29511518?v=4" width="100px;" alt=""/><br /><sub><b>Raze Soldier</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/issues?q=author%3ARazeSoldier" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/Funny-ppt"><img src="https://avatars3.githubusercontent.com/u/48616775?v=4" width="100px;" alt=""/><br /><sub><b>Funny-ppt</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/issues?q=author%3AFunny-ppt" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/dsyo2008"><img src="https://avatars2.githubusercontent.com/u/3739056?v=4" width="100px;" alt=""/><br /><sub><b>dsyo2008</b></sub></a><br /><a href="#ideas-dsyo2008" title="Ideas, Planning, & Feedback">🤔</a></td>
    <td align="center"><a href="https://github.com/ZeroSimple"><img src="https://avatars2.githubusercontent.com/u/22572927?v=4" width="100px;" alt=""/><br /><sub><b>Henry He</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/issues?q=author%3AZeroSimple" title="Bug reports">🐛</a> <a href="https://github.com/tobiichiamane/pixivfs-uwp/commits?author=ZeroSimple" title="Code">💻</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://github.com/frg2089"><img src="https://avatars0.githubusercontent.com/u/42184238?v=4" width="100px;" alt=""/><br /><sub><b>舰队的偶像-岛风酱！</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/commits?author=frg2089" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/handsome-yaokun"><img src="https://avatars1.githubusercontent.com/u/48851792?v=4" width="100px;" alt=""/><br /><sub><b>handsome-yaokun</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/issues?q=author%3Ahandsome-yaokun" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/Baka632"><img src="https://avatars1.githubusercontent.com/u/48171809?v=4" width="100px;" alt=""/><br /><sub><b>Baka632</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/issues?q=author%3ABaka632" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/pccanales"><img src="https://avatars3.githubusercontent.com/u/57200458?v=4" width="100px;" alt=""/><br /><sub><b>pccanales</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/issues?q=author%3Apccanales" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/SLK-xlw"><img src="https://avatars0.githubusercontent.com/u/57425595?v=4" width="100px;" alt=""/><br /><sub><b>SLK-xlw</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/issues?q=author%3ASLK-xlw" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/Stars-sea"><img src="https://avatars0.githubusercontent.com/u/47738830?v=4" width="100px;" alt=""/><br /><sub><b>Star sea</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/issues?q=author%3AStars-sea" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/Feng-Bu-Jue"><img src="https://avatars0.githubusercontent.com/u/31085556?v=4" width="100px;" alt=""/><br /><sub><b>FengBuJue</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/commits?author=Feng-Bu-Jue" title="Code">💻</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://www.hokori.cn"><img src="https://avatars1.githubusercontent.com/u/27987136?v=4" width="100px;" alt=""/><br /><sub><b>hokori</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/issues?q=author%3Alingxd" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/YukinoShary"><img src="https://avatars0.githubusercontent.com/u/26871617?v=4" width="100px;" alt=""/><br /><sub><b>Shary0x62</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/commits?author=YukinoShary" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/ProJend"><img src="https://avatars1.githubusercontent.com/u/55018532?v=4" width="100px;" alt=""/><br /><sub><b>ProJend</b></sub></a><br /><a href="https://github.com/tobiichiamane/pixivfs-uwp/commits?author=ProJend" title="Code">💻</a></td>
    <td align="center"><a href="http://localhost"><img src="https://avatars1.githubusercontent.com/u/24848528?v=4" width="100px;" alt=""/><br /><sub><b>いんしさくら</b></sub></a><br /><a href="#translation-Incisakura" title="Translation">🌍</a></td>
  </tr>
</table>

<!-- markdownlint-enable -->
<!-- prettier-ignore-end -->
<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!

# Special thanks

Thanks [JetBrains](https://www.jetbrains.com/?from=pixivuwp) for kindly supporting this project.

# Buy me a coffee? 

[![ko-fi](https://www.ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/P5P8WD5C)
