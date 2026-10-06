using RPGBattleMaker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Application.Interfaces
{
    public interface IEventService
    {
        EventResult GetEventResult(Event events, int selectedValue, List<Agent> teamAgents);

        int GetRandomMechanicsId();

        Event CreateFallbackEvent(int mechanicsId);
    }
}

