using Microsoft.Data.Sqlite;
using RPGBattleMaker.Application.Interfaces;

namespace RPGBattleMaker.Infrastructure.Database
{
    public class GameDatabaseInitializer : IDatabaseInitializer
    {
        private readonly string connectionString = DatabaseConfiguration.ConnectionString;
        public GameDatabaseInitializer()
        {

        }

        public async Task InitializeDatabase()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                // 1. Ativa explicitamente o suporte a Foreign Keys nesta conexão
                using (var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    await pragmaCmd.ExecuteNonQueryAsync();
                }

                // Eventos agora são gerados pela IA local.
                // Removemos a antiga tabela/seed de histórias hardcoded.
                using (var dropEventsCmd = new SqliteCommand("DROP TABLE IF EXISTS Events;", connection))
                {
                    await dropEventsCmd.ExecuteNonQueryAsync();
                }

                // 1. Criação da Tabela Agentes (Garantindo a restrição de Primary Key de forma explícita)
                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Agentes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Agente TEXT NOT NULL,
                    Tipo TEXT NOT NULL,
                    Raridade INTEGER NOT NULL,
                    Ataque INTEGER NOT NULL,
                    Defesa INTEGER NOT NULL,
                    Vida INTEGER NOT NULL,
                    Pericia INTEGER NOT NULL,
                    Sinergias TEXT
                );";

                using (var createTableCmd = new SqliteCommand(createTableQuery, connection))
                {
                    await createTableCmd.ExecuteNonQueryAsync();
                }

                // 3. Criação da Tabela Sinergias (Apontando exatamente para a coluna Id)
                string createTableSynergyQuery = @"
                CREATE TABLE IF NOT EXISTS Sinergias (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AgenteId INTEGER NOT NULL,
                    Tipo TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    FOREIGN KEY(AgenteId) REFERENCES Agentes(Id) ON DELETE CASCADE
                );";

                using (var createTableSynergyCmd = new SqliteCommand(createTableSynergyQuery, connection))
                {
                    await createTableSynergyCmd.ExecuteNonQueryAsync();
                }

                // 5. SEED: insere os heróis apenas quando a tabela ainda estiver vazia.
                using (var countAgentsCmd = new SqliteCommand("SELECT COUNT(*) FROM Agentes;", connection))
                {
                    int agentCount = Convert.ToInt32(await countAgentsCmd.ExecuteScalarAsync());
                    if (agentCount > 0)
                        return;
                }

                using (var transaction = connection.BeginTransaction())
                {
                    string insertHeroQuery = @"
                    INSERT INTO Agentes (Agente, Tipo, Raridade, Ataque, Defesa, Vida, Pericia, Sinergias) 
                    VALUES ($agente, $tipo, $raridade, $ataque, $defesa, $vida, $pericia, $sinergia);
                    SELECT last_insert_rowid();";

                    string insertSynergyQuery = @"
                    INSERT INTO Sinergias (AgenteId, Tipo, Name) 
                    VALUES ($agenteId, $tipoSinergia, $nameSinergia);";

                    using (var insertCmd = new SqliteCommand(insertHeroQuery, connection, transaction))
                    using (var insertSynergyCmd = new SqliteCommand(insertSynergyQuery, connection, transaction))
                    {
                        async Task InserirAgenteComSinergias(string nome, string tipo, int raridade, int ataque, int defesa, int vida, int pericia, string sinergiasRaw)
                        {
                            insertCmd.Parameters.Clear();
                            insertCmd.Parameters.AddWithValue("$agente", nome);
                            insertCmd.Parameters.AddWithValue("$tipo", tipo);
                            insertCmd.Parameters.AddWithValue("$raridade", raridade);
                            insertCmd.Parameters.AddWithValue("$ataque", ataque);
                            insertCmd.Parameters.AddWithValue("$defesa", defesa);
                            insertCmd.Parameters.AddWithValue("$vida", vida);
                            insertCmd.Parameters.AddWithValue("$pericia", pericia);
                            insertCmd.Parameters.AddWithValue("$sinergia", sinergiasRaw);

                            long agenteId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync());

                            if (!string.IsNullOrEmpty(sinergiasRaw))
                            {
                                string[] sinergias = sinergiasRaw.Split(',');
                                foreach (var sinergia in sinergias)
                                {
                                    insertSynergyCmd.Parameters.Clear();
                                    insertSynergyCmd.Parameters.AddWithValue("$agenteId", agenteId);
                                    insertSynergyCmd.Parameters.AddWithValue("$tipoSinergia", tipo);
                                    insertSynergyCmd.Parameters.AddWithValue("$nameSinergia", sinergia.Trim());
                                    await insertSynergyCmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        // O restante da lista dos heróis permanece idêntica abaixo...
                        await InserirAgenteComSinergias("Kazumi", "Especialista", 5, 4, 4, 7, 13, "Hara-Kiri,Líder,Solo,Irmãs");
                        await InserirAgenteComSinergias("Perdigas", "Suporte", 5, 2, 2, 8, 16, "Ordem,Fé");
                        await InserirAgenteComSinergias("Maria Cecília", "Defensor", 5, 2, 16, 8, 2, "Ordem,Nova Ordem");
                        await InserirAgenteComSinergias("Crane", "Lutador", 5, 17, 2, 7, 2, "Solo");
                        await InserirAgenteComSinergias("Lilith", "Especialista", 5, 4, 4, 7, 13, "Vilão,Solo");
                        await InserirAgenteComSinergias("Agni", "Suporte", 4, 2, 2, 6, 12, "A chama");
                        await InserirAgenteComSinergias("Akane", "Lutador", 4, 13, 2, 5, 2, "Irmãs,Nova Ordem");
                        await InserirAgenteComSinergias("Tom", "Lutador", 4, 13, 2, 5, 2, "Nova Ordem");
                        await InserirAgenteComSinergias("Aziza", "Lutador", 4, 13, 2, 5, 2, "A chama");
                        await InserirAgenteComSinergias("Shiva", "Especialista", 4, 3, 3, 5, 11, "Irmãos,Caos");
                        await InserirAgenteComSinergias("Aya", "Suporte", 4, 2, 3, 7, 10, "Ninho do Dragão,Caos");
                        await InserirAgenteComSinergias("Caim", "Defensor", 4, 2, 13, 6, 1, "Vilão");
                        await InserirAgenteComSinergias("Viktor", "Defensor", 4, 2, 13, 6, 1, "Uagamora");
                        await InserirAgenteComSinergias("Gabriel", "Especialista", 4, 2, 1, 6, 13, "Nova Ordem");
                        await InserirAgenteComSinergias("Thoryn", "Defensor", 3, 1, 12, 5, 0, "Trindade,Fé");
                        await InserirAgenteComSinergias("Nix", "Suporte", 3, 1, 1, 5, 11, "Fé");
                        await InserirAgenteComSinergias("Zendaya", "Suporte", 3, 1, 1, 5, 11, "Trindade");
                        await InserirAgenteComSinergias("Daerion", "Defensor", 3, 3, 7, 6, 2, "Casal Real,Amor platônico,Ninho do Dragão,Dragão");
                        await InserirAgenteComSinergias("Shyvana", "Lutador", 3, 11, 1, 5, 1, "Uagamora,Dragão");
                        await InserirAgenteComSinergias("Symon", "Suporte", 3, 1, 1, 5, 11, "Uagamora");
                        await InserirAgenteComSinergias("Kael", "Lutador", 3, 11, 1, 5, 1, "Uagamora");
                        await InserirAgenteComSinergias("Arkmeros", "Suporte", 3, 1, 1, 5, 11, "Fé");
                        await InserirAgenteComSinergias("Ruivo", "Especialista", 3, 2, 0, 5, 11, "Vendedores,Ninho do Dragão");
                        await InserirAgenteComSinergias("Marionetista", "Especialista", 3, 2, 0, 5, 11, "Vilão");
                        await InserirAgenteComSinergias("Seph Flores", "Especialista", 3, 2, 0, 5, 11, "Hara-Kiri");
                        await InserirAgenteComSinergias("Barbara", "Lutador", 3, 11, 1, 5, 1, "Trindade");
                        await InserirAgenteComSinergias("Saphyra", "Lutador", 2, 9, 1, 3, 1, "Amor");
                        await InserirAgenteComSinergias("Oriven", "Especialista", 2, 2, 0, 4, 8, "Uagamora");
                        await InserirAgenteComSinergias("Megan", "Suporte", 2, 2, 0, 4, 8, "Hara-Kiri");
                        await InserirAgenteComSinergias("Cael", "Lutador", 2, 8, 1, 4, 1, "Ninho do Dragão");
                        await InserirAgenteComSinergias("Sirius", "Especialista", 2, 2, 0, 4, 8, "Caos");
                        await InserirAgenteComSinergias("Marcos", "Defensor", 2, 1, 8, 4, 1, "Hara-Kiri");
                        await InserirAgenteComSinergias("Shantal", "Suporte", 2, 1, 1, 4, 8, "A chama,Amor");
                        await InserirAgenteComSinergias("Lamblin", "Lutador", 2, 9, 1, 3, 1, "Solo");
                        await InserirAgenteComSinergias("Aryte", "Defensor", 2, 1, 8, 4, 1, "Uagamora");
                        await InserirAgenteComSinergias("Zahra", "Defensor", 2, 1, 8, 4, 1, "Vendedores");
                        await InserirAgenteComSinergias("Samir", "Lutador", 2, 6, 2, 3, 3, "Irmãos,Caos");
                        await InserirAgenteComSinergias("Nyu", "Especialista", 2, 2, 0, 4, 8, "Hara-Kiri");
                        await InserirAgenteComSinergias("Padre", "Suporte", 2, 1, 0, 4, 9, "Caos,Fé");
                        await InserirAgenteComSinergias("Orion", "Suporte", 1, 1, 1, 3, 5, "Fé");
                        await InserirAgenteComSinergias("Kru'el", "Especialista", 1, 1, 0, 3, 6, "Hara-Kiri");
                        await InserirAgenteComSinergias("Meilyn", "Lutador", 1, 6, 1, 3, 0, "Amor platônico,Ninho do Dragão");
                        await InserirAgenteComSinergias("Mauga", "Defensor", 1, 1, 6, 3, 0, "Uagamora");
                        await InserirAgenteComSinergias("Marcus", "Defensor", 1, 1, 6, 3, 0, "Líder,Nova Ordem");
                        await InserirAgenteComSinergias("Leão", "Especialista", 1, 1, 0, 3, 6, "Líder,Ordem");
                        await InserirAgenteComSinergias("Matheus", "Lutador", 1, 6, 1, 3, 0, "Nova Ordem");
                        await InserirAgenteComSinergias("Cronista", "Especialista", 1, 1, 0, 3, 6, "Uagamora");
                        await InserirAgenteComSinergias("Vysenia", "Lutador", 1, 4, 2, 2, 2, "Casal Real,Líder,Ninho do Dragão,Dragão");
                        await InserirAgenteComSinergias("Bree", "Suporte", 1, 1, 0, 3, 6, "Vendedores");
                        await InserirAgenteComSinergias("Mimoso", "Lutador", 1, 6, 1, 3, 0, "Vendedores");
                        await InserirAgenteComSinergias("Don Omar", "Especialista", 1, 1, 0, 3, 6, "Vendedores");
                        await InserirAgenteComSinergias("Yasmin", "Suporte", 1, 1, 1, 3, 5, "Vendedores,Fé");
                        await InserirAgenteComSinergias("Kyra", "Suporte", 1, 1, 1, 3, 5, "Uagamora");

                        await transaction.CommitAsync();
                    }
                }
            }
        }
    }
}
