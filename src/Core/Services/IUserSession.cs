using System.Threading.Tasks;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services
{
	public interface IUserSession
	{
		Task<Employee?> GetCurrentUserAsync();
    }
}