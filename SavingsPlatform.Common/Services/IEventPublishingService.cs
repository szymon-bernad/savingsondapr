using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Services
{
    public interface IEventPublishingService
    {
        Task PublishEvents(ICollection<object> events);

        Task PublishCommand<T>(T command);
    }
}
