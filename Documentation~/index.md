# [ilib-serv-injector](https://github.com/yazawa-ichio/ilib-serv-injector)

Unity Service Locater And Injector Package.

リポジトリ https://github.com/yazawa-ichio/ilib-serv-injector

## 概要

サービスロケーターに依存関係の注入機能を追加したもの。  
DIコンテナとサービスロケーターの中間ぐらい。  
機能よりも軽量に動作することを目標に作成。

## 使用方法

### サービスの作成

最初に登録するサービス機能を持つ`interface`を作成します。  
後はそのインターフェースを実装したクラスを作成するだけです。  
サービスを利用する際は必ずインターフェースを通して処理を行うため、必要な機能は全てinterfaceに入れる必要があります。

```csharp
using UnityEngine;
using System;

public interface ILogService
{
	void Debug(string msg);
	void Warning(string msg);
	void Error(string msg);
}

public class UnityLogService : ILogService
{
	public void Debug(string msg) => Debug.Log(msg);
	public void Warning(string msg) => Debug.Warning(msg);
	public void Error(string msg) => Debug.Error(msg);
}

public class ConsoleLogService : ILogService
{
	public void Debug(string msg) => Console.WriteLine("[Debug]"+msg);
	public void Warning(string msg) => Console.WriteLine("[Warning]"+msg);
	public void Error(string msg) => Console.WriteLine("[Error]"+msg);
}
```

### サービスの登録・利用

サービスに関数する殆どの処理は`ServInjector`クラスを通して行われます。  
サービスを登録する場合は`ServInjector.Bind<T>`を使用します。  
逆にサービスの登録を解除する場合は`ServInjector.Unbind<T>`を使用します。  

`MonoBehaviour`を継承したサービスの場合は、`ServInjector.BindAndObserveDestroy` を使用するとUnityのライフサイクルで破棄された際に、自動で解放されます。  
ただし、可能であれば後述の`ServiceMonoBehaviour` を継承する方法を利用してください。

サービスを利用する際は`ServInjector.Resolve<T>`で取得できます。

```csharp
using ILib.ServInject;

void Proc()
{
	//サービスを登録する
#if CONSOLE_APP
	//Consoleアプリケーション使用する場合
	ServInjector.Bind<ILogService>(new ConsoleLogService());
#else
	//Unity使用する場合
	ServInjector.Bind<ILogService>(new UnityLogService());
#endif

	// サービスの取得
	var log = ServInjector.Resolve<ILogService>();
	log.Debug("利用側はどこに出力するか意識しないでログを使用できる");

	//Unbindで解除出来ます。
	//現在バインドされているサービス明示的に消したいため、必ず現在のインスタンスを指定する必要がある。
	ServInjector.Unbind<ILogService>(log);

}
```

#### サービスをすべて解除する

`ServInjector.Clear`関数を実行すると現在登録されている全てのサービスを解除します。

#### 自動登録するクラス

`Service`と`ServiceMonoBehaviour`を継承してクラスは、インスタンスの作成時に自身を `ServInjector`に登録します。  
登録を解除する場合は`Service`クラスは`Dispose`関数、`ServiceMonoBehaviour`はDestroyしてください。


```csharp
public class UnityLogService : ServiceMonoBehaviour<ILogService>, ILogService
{
	public void Debug(string msg) => Debug.Log(msg);
	public void Warning(string msg) => Debug.Warning(msg);
	public void Error(string msg) => Debug.Error(msg);
}

public class ConsoleLogService : Service<ILogService>, ILogService
{
	public void Debug(string msg) => Console.WriteLine("[Debug]"+msg);
	public void Warning(string msg) => Console.WriteLine("[Warning]"+msg);
	public void Error(string msg) => Console.WriteLine("[Error]"+msg);
}
```

### サービスを注入する

通常のDIコンテナではより複雑な処理が可能ですが、このパッケージでは登録済みのサービスをセットする以上の機能を持ちません。  
どちらかと言えば、クラスがどのサービスを使用したいかを明示的にするぐらいの機能です。  
`ServInjector.Inject` 関数でサービスを注入できます。  
注入するサービスの指定は後述の項目で説明します。  

```csharp
using ILib.ServInject;

void Proc()
{
	var instance = new Sample();
	ServInjector.Inject(instance);

	//new ()の型であればサービスの注入を行ったインタンスを返すCrete関数が使用できる
	instance = ServInjector.Create<Sample>();
}
```

#### インターフェースによる注入

インターフェスによる注入です。  
型解決で実行されるため、比較的高速に動作します。  
基本的にこのインターフェスによる注入で実装してください。

```csharp
using ILib.ServInject;

class Sample : IInject<ILogService>
{
	ILogService m_Log;
	public void Install(ILogService service) => m_Log = service;
}
```

#### リファレクションによる注入

リファレクションを利用した注入です。  
`InjectAttribute`をプロパティか関数につける事で`ServInjector.Inject`実行時にサービスのインタンスが渡ります。  
`public`プロパティと関数のみが対象です。  

リファレクションによる注入を一切行わない場合は`ILIB_DISABLE_SERV_REFLECTION_INJECT`シンボルを定義することで機能を無効にできます。

```csharp
class Sample
{
	ILogService m_Log;

	//プロパティの場合はsetが呼ばれる。
	[Inject]
	public ILogService Log { get; set; }

	[Inject]
	public void Install(ILogService service) => m_Log = service;
}
```

#### その他

`Inject`の実行には属性などのメタ情報を型ごと取得します。  
しかし、継承したクラスなどで、すべてメタ情報が基底クラスにしか存在しない場合があります。  
その際、Inject時に基底クラスの`System.Type`を使用する事で、メタ情報を共有することが出来ます。


## LICENSE

https://github.com/yazawa-ichio/ilib-serv-injector/blob/master/LICENSE