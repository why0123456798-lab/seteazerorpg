using RPGBattleMaker.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Data.Interface
{
    public interface IEventRepository
    {
        Task<List<Event>> GetAllEvents();
    }
}
