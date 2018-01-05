# 资源更新 HotFix

Unity资源更新方案。HoxFix实现了一套资源更新流程，可以对工程中的资源做自动批量打包标识，自动完成打包过程。对打包后的资源，使用了7zip进行了资源压缩，上传服务器。对每一个资源包使用单独的版本号码，和哈希码标识，只打包和下载更新修改过的资源。资源更新过程中，根据每个资源包的版本号码，进行增量更新。使用多线程下载、解压资源，防止阻塞UI线程。

## 开发测试环境

Mac 10.12.6

Unity 5.6.0p4


## 如何使用

### 资源打包

1.对要需要打包的资源进行标识。也可以使用编辑器代码进行自动标记。可以配置 AssetBundleBuilder.cs 文件中的 pathArray 数组，脚本会对数组中的目录进行标识。最终的资源包名为  路径名+.n。 （Unity 的assetbundle 不支持大写）

```
	    /// <summary>
	    /// 配置要打包的资源路径
		/// 资源包的名称= 路径名+.n
	    /// </summary>
		static string[] pathArray = {
			"BundleHotFix/TestBundle",
	
		};
```

2.执行 `菜单->AssetBundle->Build AssetBundle (资源打包) `

3.打包资源在  `工程目录/AssetBundles/Android`   (android 平台)
 


### 资源压缩

执行菜单 ->AssetBundle -> CompressFile(打包成服务器资源)

压缩文件在 工程目录/AssetBundles/ServerCompressAssets

### 把资源上传到服务器

把上一步的压缩文件上传到nginx 服务器。 可以使用FileZila. 服务器的配置可以参考 配置nginx 作为文件服务器。

### 资源下载更新

把目录 BundleHotFix 下的 预设UpdateAssetBundle 拖入到场景中，并设置好资源服务器地址。 就可以运行游戏了。 


------------------------

### AssetBunle资源加载

AssetBundle 资源加载可以参考 BundleHotFix/Script/LoadAssetsManager.cs 。

## 配置nginx 作为文件服务器

服务器环境Ubuntu16.04.

1 安装 

```
apt-get install nginx

```

2 创建一个新的虚拟机配置文件

```
nano /etc/nginx/site-enable/fileserver.conf

```
3 文件配置

```
server {
    listen  8080;
    server_name   45.76.191.xx; # 自己PC的ip或者服务器的域名
    charset utf-8; # 避免中文乱码
    root /root/assetbundle; # 存放文件的目录
    location / {
        autoindex on; # 索引
        autoindex_exact_size on; # 显示文件大小
        autoindex_localtime on; # 显示文件时间
    }
}

```

4 删除default ,修改 /etc/nginx/nginx.conf user

```
rm /etc/nginx/site-enabled/default
```

修改 /etc/nginx/nginx.conf 

```
user youmeng; //youmeng 当前的用户名称
```

设置目录的权限

```
chmod -R 775 /path  // path 文件路径
```

5 启动nginx 或者 载入配置文件

```
/etc/init.d/nginx start|restart|reload

```

## 下载更新过程说明

此过程在脚本文件 UpdateAssetBundle.cs 中执行。

1.对比资源版本号。版本信息由vercode 和 vername 两个属性，一个数字一个字符串。 版本名称vername 格式为 X.Y.Z ,X表示大版本号，表示前后是不兼容的版本，Y表示有功能上的修改，z表示修复bug。

```
本地的版本名称和服务器的版本名称对比时，x 不相等时不再进行更新。
还要对比vercode ,服务器的  vercode 比较大的情况下才更新。
```

2.资源文件检查，检查每一个资源是否有新版本，若有就加入下载列表。

3.下载文件。在电脑上，下载文件会存放在 工程根目录下的ServerExtensionData目录。 Android和IOS平台的则会存放在内部存储的ServerExtensionData目录

4.解压文件

5.更新文件的信息，防止重复下载。

6.结束下载过程。新下载的文件后缀名为.temp。

7.全部的下载，解压完成后，用后缀名为.temp的文件替换原来的旧资源。

8.调用GameStart 的方法正式加载游戏。


## 引用插件

1.JsonDotNet JSON 文件解释 

2.7zip  文件压缩 解压

3.Loom 线程处理


## 文件简述

1.AssetBundle\ResourceInfoData.csv 。AssetBundle 文件信息列表，记录每个文件的版本号，hash码。

2.AssetBundle\resVer.txt 记录服务器资源的总体版本号码vercode 和版本名称vername。

3.Resources\Data\Josn\ResVersionData, 记录本地资源的版本号码vercode 和版本名称vername。

4.Resources/Data/ResourceInfoData.csv 文件信息的模板。

_______________

执行完资源打包之后，打包资源中有两个文件。

resVer.txt  当前资源包的版本信息，只有资源有修改的情况下，版本号码才会递增。

ResourceInfoData.csv  每一个资源包的版本号码和哈希码。


## 测试文件

BundleHotFix/TestBundle 目录是测试资源

BundleHotFix/Scene/Test.unity 是示例场景

