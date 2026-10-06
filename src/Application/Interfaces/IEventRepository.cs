using RPGBattleMaker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Application.Interfaces
{
    public interface IEventRepository
    {
        Task<List<Event>> GetAllEvents();
    }
}

