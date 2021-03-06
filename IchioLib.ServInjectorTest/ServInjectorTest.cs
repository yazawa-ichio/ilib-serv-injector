﻿using ILib.ServInject;
using NUnit.Framework;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

interface ITestService
{
	string Name { get; }
}

interface IDelayService
{

}


class TestService1 : Service<ITestService>, ITestService
{
	public string Name => typeof(TestService1).Name;
}

class TestService2 : ITestService
{
	public string Name => typeof(TestService2).Name;
}

class DelayService : IDelayService
{

}

class TestClient : IInject<ITestService>, IInject<IDelayService>
{
	public ITestService Service;
	public IDelayService Delay;
	void IInject<ITestService>.Install(ITestService service) => Service = service;
	void IInject<IDelayService>.Install(IDelayService service) => Delay = service;
}

class TestClientNest : TestClient
{
	[Inject]
	public ITestService Service2 { get; set; }

	public ITestService Service3 { get; set; }

	[Inject]
	public void Inject(ITestService service) => Service3 = service;
}

class TestServiceBehaviour : ServiceMonoBehaviour<ITestService>, ITestService
{
	public string Name => typeof(TestServiceBehaviour).Name;
}

class TestClientBehaviour : MonoBehaviour, IInject<ITestService>
{
	public ITestService Service;
	void IInject<ITestService>.Install(ITestService service) => Service = service;
}

public class ServInjectorTest
{
	[Test]
	public void ServiceLocaterTest()
	{
		ServInjector.Clear();

		//Serviceを継承した場合は自動で登録される
		var serv1 = new TestService1();
		Assert.AreEqual(ServInjector.Resolve<ITestService>(), serv1);
		//DisposeでUnbindされる
		serv1.Dispose();
		Assert.IsNull(ServInjector.Resolve<ITestService>());
		//手動で登録してみる
		ServInjector.Bind<ITestService>(serv1);
		Assert.AreEqual(ServInjector.Resolve<ITestService>(), serv1);
		//別のサービスをバインドする
		var serv2 = new TestService2();
		ServInjector.Bind<ITestService>(serv2);
		Assert.AreNotEqual(ServInjector.Resolve<ITestService>(), serv1);
		Assert.AreEqual(ServInjector.Resolve<ITestService>(), serv2);
		//service1はバインドされていないので解放しても結果は同じ
		serv1.Dispose();
		Assert.AreNotEqual(ServInjector.Resolve<ITestService>(), serv1);
		Assert.AreEqual(ServInjector.Resolve<ITestService>(), serv2);
		//手動で解放してみる
		ServInjector.Unbind<ITestService>(serv2);
		Assert.IsNull(ServInjector.Resolve<ITestService>());
	}

	[Test]
	public void ServiceLocaterClearTest()
	{
		ServInjector.Clear();

		//複数のサービスを登録
		var serv1 = new TestService1();
		ServInjector.Bind<ITestService>(serv1);
		ServInjector.Bind<TestService1>(serv1);
		ServInjector.Bind<TestService2>(new TestService2());
		//取り出せる
		Assert.IsNotNull(ServInjector.Resolve<ITestService>());
		Assert.IsNotNull(ServInjector.Resolve<TestService1>());
		Assert.IsNotNull(ServInjector.Resolve<TestService2>());
		//Clearですべてにcacheが削除される
		ServInjector.Clear();
		Assert.IsNull(ServInjector.Resolve<ITestService>());
		Assert.IsNull(ServInjector.Resolve<TestService1>());
		Assert.IsNull(ServInjector.Resolve<TestService2>());
	}


	[Test]
	public void InjectInterfaceTest()
	{
		ServInjector.Clear();
		var serv1 = new TestService1();
		Assert.AreEqual(ServInjector.Resolve<ITestService>(), serv1);
		//インジェクトして作成
		var client = ServInjector.Create<TestClient>();
		Assert.AreEqual(client.Service, serv1);

		//インジェクト関数でも追加できる
		ServInjector.Bind<ITestService>(new TestService2());
		ServInjector.Inject(client);
		Assert.AreEqual(client.Service.Name, typeof(TestService2).Name);

		//型を指定した場合、その型でInjectされる
		var nest = ServInjector.Create<TestClientNest>(typeof(TestClient));
		Assert.AreEqual(nest.Service.Name, typeof(TestService2).Name);
		Assert.AreNotEqual(nest.Service, nest.Service2);
		Assert.AreNotEqual(nest.Service, nest.Service3);

		//型を指定していないので継承先のプロパティ・メソッドにインジェクトされる
		ServInjector.Inject(nest);
		Assert.AreEqual(nest.Service, nest.Service2);
		Assert.AreEqual(nest.Service, nest.Service3);

	}

	[UnityTest]
	public IEnumerator InjectMonoBehaviourTest()
	{
		GameObject obj = new GameObject("Test");
		try
		{
			//ServiceBehaviourは勝手に登録される
			var serv = obj.AddComponent<TestServiceBehaviour>();
			Assert.AreEqual(ServInjector.Resolve<ITestService>(), serv);
			//AddComponent時にインジェクト
			var client = ServInjector.AddComponent<TestClientBehaviour>(obj);
			Assert.AreEqual(ServInjector.Resolve<ITestService>(), client.Service);
			Component.Destroy(serv);
			//OnDestroyを待つ
			yield return null;
			Assert.IsNull(ServInjector.Resolve<ITestService>());
		}
		finally
		{
			GameObject.Destroy(obj);
		}
	}

	[Test]
	public void InjectDelayTest()
	{
		ServInjector.Clear();
		ServInjector.Bind<ITestService>(new TestService1());
		//型情報がメタ情報が作成される
		var client = ServInjector.Create<TestClient>();
		Assert.IsNotNull(client.Service);
		Assert.IsNull(client.Delay);

		//メタ情報が出来た後でも後からでもバインド出来る
		ServInjector.Bind<IDelayService>(new DelayService());
		client = ServInjector.Create<TestClient>();
		Assert.IsNotNull(client.Service);
		Assert.IsNotNull(client.Delay);

	}

	[Test]
	public void ResolveAsyncTest()
	{
		Task.Run(async () =>
		{
			ServInjector.Clear();
			_ = Task.Run(async () =>
			{
				await Task.Delay(2000);
				ServInjector.Bind<ITestService>(new TestService1());
			});
			Exception error = null;
			try
			{
				var cancel = await ServInjector.ResolveAsync<ITestService>(new CancellationTokenSource(500).Token);
				Assert.IsNull(cancel);
			}
			catch (Exception ex) { error = ex; }
			Assert.IsNotNull(error);
			Assert.IsNull(ServInjector.Resolve<ITestService>());
			var service = await ServInjector.ResolveAsync<ITestService>();
			Assert.IsNotNull(service);
		}).Wait();
	}
}