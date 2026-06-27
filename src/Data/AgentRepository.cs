using Microsoft.Data.Sqlite;
using RPGBattleMaker.Data.Interface;
using System;
using System.Collections.Generic;
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
                string selectQuery = "SELECT Agente, Tipo, Raridade, Sinergias, Ataque, Defesa, Vida, Pericia FROM Agentes;";

                using (var selectCmd = new SqliteCommand(selectQuery, connection))
                using (var reader = await selectCmd.ExecuteReaderAsync())
                {
                    allAgents.Clear(); // Limpa a lista antes de recarregar

                    while (await reader.ReadAsync())
                    {
                        // Resgata os dados mapeados na ordem exata do SELECT acima
                        string name = reader.GetString(0);
                        string type = reader.GetString(1);
                        int rarity = reader.GetInt32(2);
                        string synergyText = reader.IsDBNull(3) ? "" : reader.GetString(5);
                        int ataque = reader.GetInt32(4);
                        int defesa = reader.GetInt32(5);
                        int vida = reader.GetInt32(6);
                        int pericia = reader.GetInt32(7);

                        // Instancia o agente usando o novo construtor limpo!
                        Agent agent = new Agent(name, type, rarity, synergyText, ataque, defesa, vida, pericia);

                        allAgents.Add(agent);
                    }
                }
            }
        }

        public async Task<List<string>> GetHeroSynergies(string heroName)
        {
            List<string> synergies = new List<string>();

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                var selectQuery = "SELECT Sinergias FROM Agentes WHERE Agente = $nome;";

                using (var selectCmd = new SqliteCommand(selectQuery, connection))
                {
                    selectCmd.Parameters.AddWithValue("$nome", heroName);

                    using (var reader = await selectCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync() && !reader.IsDBNull(0))
                        {
                            string sinergiasTexto = reader.GetString(0);

                            // Faz o Split eliminando espaços ou entradas vazias
                            synergies = sinergiasTexto
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .ToList();
                        }
                    }
                }

                return synergies;
            }
        }

        public async Task<Agent> GetHeroByName(string agent)
        {

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                string selectQuery = "SELECT Agente, Tipo, Raridade, Sinergias, Ataque, Defesa, Vida, Pericia FROM Agentes WHERE Agente = @heroName;";

                using (var selectCmd = new SqliteCommand(selectQuery, connection))
                {
                    selectCmd.Parameters.AddWithValue("@heroName", agent);

                    using (var reader = await selectCmd.ExecuteReaderAsync())
                    {
                        // Resgata os dados mapeados na ordem exata do SELECT acima
                        string name = reader.GetString(0);
                        string type = reader.GetString(1);
                        int rarity = reader.GetInt32(4);
                        string synergyText = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        int ataque = reader.GetInt32(6);
                        int defesa = reader.GetInt32(7);
                        int vida = reader.GetInt32(8);
                        int pericia = reader.GetInt32(9);

                        return new Agent(name, type, rarity, synergyText, ataque, defesa, vida, pericia);

                    }
                }
            }
        }
    }
}
