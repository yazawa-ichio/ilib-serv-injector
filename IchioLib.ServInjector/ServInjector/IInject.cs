namespace ILib.ServInject
{
	public interface IInject<T> where T : class
	{
		void Install(T service);
	}
}