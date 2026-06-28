using Microsoft.Data.Sqlite;
using RPGBattleMaker.Data.Interface;
using RPGBattleMaker.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Data
{
    public class EventRepository : IEventRepository
    {
        private readonly string connectionString = "Data Source=rpg_battle.db";
        public EventRepository()
        {

        }

        public async Task<List<Event>> GetAllEvents()
        {
            var allEvents = new List<Event>();  
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                string selectQuery = "SELECT Id, Name, Description, OptionA, OptionB, OptionC FROM Events;";

                using (var selectCmd = new SqliteCommand(selectQuery, connection))
                using (var reader = await selectCmd.ExecuteReaderAsync())
                {

                    while (await reader.ReadAsync())
                    {
                        // Resgata os dados mapeados na ordem exata do SELECT acima
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        string description = reader.GetString(2);
                        string optionA = reader.GetString(3);
                        string optionB = reader.GetString(4);
                        string optionC = reader.GetString(5);

                        // Instancia o agente usando o novo construtor limpo!
                        Event events = new Event(id, name, description, optionA, optionB, optionC);

                        allEvents.Add(events);
                    }
                }
            }
            return allEvents;
        }
    }
}
