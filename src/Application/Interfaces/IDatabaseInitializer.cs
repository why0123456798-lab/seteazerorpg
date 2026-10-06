using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Application.Interfaces
{
    public interface IDatabaseInitializer
    {
        Task InitializeDatabase();
    }
}

