namespace ILib.ServInject
{
	public interface IServiceEventReceiver
	{
		void OnBind();
		void OnUnbind();
	}
}