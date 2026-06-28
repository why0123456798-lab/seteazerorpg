using Microsoft.Data.Sqlite;
using RPGBattleMaker.Data.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace RPGBattleMaker.Data
{
    public class AgentRepository : IAgentRepository
    {
        private readonly string connectionString = "Data Source=rpg_battle.db";
        public AgentRepository()
        {

        }
        public async Task GetAllHeroes(List<Agent> allAgents)
        {

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                string selectQuery = "SELECT Id, Agente, Tipo, Raridade, Sinergias, Ataque, Defesa, Vida, Pericia FROM Agentes;";

                using (var selectCmd = new SqliteCommand(selectQuery, connection))
                using (var reader = await selectCmd.ExecuteReaderAsync())
                {
                    allAgents.Clear(); // Limpa a lista antes de recarregar

                    while (await reader.ReadAsync())
                    {
                        // Resgata os dados mapeados na ordem exata do SELECT acima
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        string type = reader.GetString(2);
                        int rarity = reader.GetInt32(3);
                        string synergyText = reader.IsDBNull(4) ? "" : reader.GetString(5);
                        int ataque = reader.GetInt32(5);
                        int defesa = reader.GetInt32(6);
                        int vida = reader.GetInt32(7);
                        int pericia = reader.GetInt32(8);

                        // Instancia o agente usando o novo construtor limpo!
                        Agent agent = new Agent(id, name, type, rarity, synergyText, ataque, defesa, vida, pericia);

                        allAgents.Add(agent);
                    }
                }
            }
        }

        public async Task<List<string>> GetHeroSynergies(int heroId)
        {
            List<string> synergies = new List<string>();

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                // O SELECT continua igual, buscando o Name de todas as linhas do AgenteId informado
                var selectQuery = "SELECT Name FROM Sinergias WHERE AgenteId = $agentId;";

                using (var selectCmd = new SqliteCommand(selectQuery, connection))
                {
                    selectCmd.Parameters.AddWithValue("$agentId", heroId);

                    using (var reader = await selectCmd.ExecuteReaderAsync())
                    {
                        // Mudamos de "if" para "while", pois o banco retornará múltiplas linhas (uma para cada sinergia)
                        while (await reader.ReadAsync())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                // Adiciona a sinergia da linha atual diretamente na lista
                                synergies.Add(reader.GetString(0));
                            }
                        }
                    }
                }

                return synergies;
            }
        }

        public async Task<Agent> GetHeroById(int heroId)
        {

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                string selectQuery = "SELECT Id, Agente, Tipo, Raridade, Sinergias, Ataque, Defesa, Vida, Pericia FROM Agentes WHERE Id = @heroId;";

                using (var selectCmd = new SqliteCommand(selectQuery, connection))
                {
                    selectCmd.Parameters.AddWithValue("@heroId", heroId);

                    using (var reader = await selectCmd.ExecuteReaderAsync())
                    {
                        // Resgata os dados mapeados na ordem exata do SELECT acima
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        string type = reader.GetString(2);
                        int rarity = reader.GetInt32(3);
                        string synergyText = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        int ataque = reader.GetInt32(5);
                        int defesa = reader.GetInt32(6);
                        int vida = reader.GetInt32(7);
                        int pericia = reader.GetInt32(8);

                        return new Agent(id, name, type, rarity, synergyText, ataque, defesa, vida, pericia);

                    }
                }
            }
        }
    }
}
