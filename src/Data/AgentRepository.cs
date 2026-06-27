using Microsoft.Data.Sqlite;
using RPGBattleMaker.Data.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Data 
{
    public class AgentRepository : IAgentRepository
    {
        public AgentRepository()
        {

        }
        public async Task GetAllHeroes(List<Agent> allAgents)
        {
            // O banco de dados será criado automaticamente na memória ou em um arquivo local na bin
            string connectionString = "Data Source=rpg_battle.db";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Agente, Tipo, Descricao, Player, Raridade, Sinergia, Ataque, Defesa, Vida, Pericia FROM Agentes;";

                using (var selectCmd = new SqliteCommand(selectQuery, connection))
                using (var reader = selectCmd.ExecuteReader())
                {
                    allAgents.Clear(); // Limpa a lista antes de recarregar

                    while (reader.Read())
                    {
                        // Resgata os dados mapeados na ordem exata do SELECT acima
                        string name = reader.GetString(0);
                        string type = reader.GetString(1);
                        string desc = reader.IsDBNull(2) ? "" : reader.GetString(2); // Evita erro se a descrição estiver em branco
                        string player = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        int rarity = reader.GetInt32(4);
                        string synergyText = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        int ataque = reader.GetInt32(6);
                        int defesa = reader.GetInt32(7);
                        int vida = reader.GetInt32(8);
                        int pericia = reader.GetInt32(9);

                        // Instancia o agente usando o novo construtor limpo!
                        Agent agent = new Agent(name, type, desc, player, rarity, synergyText, ataque, defesa, vida, pericia);

                        allAgents.Add(agent);
                    }
                }
            }
        }
    }
}
