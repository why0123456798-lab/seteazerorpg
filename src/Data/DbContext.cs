using Microsoft.Data.Sqlite;
using RPGBattleMaker.Data.Interface;

namespace RPGBattleMaker.Data
{
    public class DbContext : IDbContext
    {
        private readonly string connectionString = "Data Source=rpg_battle.db";
        public DbContext()
        {

        }

        public async Task InitializeDatabase()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // 1. Ativa explicitamente o suporte a Foreign Keys nesta conexão
                using (var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    pragmaCmd.ExecuteNonQuery();
                }

                // 2. Criação da Tabela Agentes (Garantindo a restrição de Primary Key de forma explícita)
                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Agentes (
                    Id INTEGER NOT NULL,
                    Agente TEXT, 
                    Tipo TEXT, 
                    Raridade INTEGER, 
                    Ataque INTEGER, 
                    Defesa INTEGER, 
                    Vida INTEGER, 
                    Pericia INTEGER,  
                    Sinergias TEXT,
                    PRIMARY KEY(Id AUTOINCREMENT)
                );";

                using (var createTableCmd = new SqliteCommand(createTableQuery, connection))
                {
                    createTableCmd.ExecuteNonQuery();
                }

                // 3. Criação da Tabela Sinergias (Apontando exatamente para a coluna Id)
                string createTableSynergyQuery = @"
                CREATE TABLE IF NOT EXISTS Sinergias (
                    Id INTEGER NOT NULL,
                    AgenteId INTEGER,
                    Tipo TEXT, 
                    Name TEXT,
                    PRIMARY KEY(Id AUTOINCREMENT),
                    FOREIGN KEY(AgenteId) REFERENCES Agentes(Id) ON DELETE CASCADE
                );";

                using (var createTableSynergyCmd = new SqliteCommand(createTableSynergyQuery, connection))
                {
                    createTableSynergyCmd.ExecuteNonQuery();
                }

                // 4. RESETA O BANCO
                using (var deleteCmd = new SqliteCommand("DELETE FROM Sinergias;", connection))
                {
                    deleteCmd.ExecuteNonQuery();
                }

                using (var deleteCmd = new SqliteCommand("DELETE FROM Agentes;", connection))
                {
                    deleteCmd.ExecuteNonQuery();
                }

                using (var sqliteSequenceCmd = new SqliteCommand("DELETE FROM sqlite_sequence WHERE name='Agentes' OR name='Sinergias';", connection))
                {
                    sqliteSequenceCmd.ExecuteNonQuery();
                }

                // 5. SEED: Insere os valores via transação
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
                        void InserirAgenteComSinergias(string nome, string tipo, int raridade, int ataque, int defesa, int vida, int pericia, string sinergiasRaw)
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

                            long agenteId = (long)insertCmd.ExecuteScalar();

                            if (!string.IsNullOrEmpty(sinergiasRaw))
                            {
                                string[] sinergias = sinergiasRaw.Split(',');
                                foreach (var sinergia in sinergias)
                                {
                                    insertSynergyCmd.Parameters.Clear();
                                    insertSynergyCmd.Parameters.AddWithValue("$agenteId", agenteId);
                                    insertSynergyCmd.Parameters.AddWithValue("$tipoSinergia", tipo);
                                    insertSynergyCmd.Parameters.AddWithValue("$nameSinergia", sinergia.Trim());
                                    insertSynergyCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // O restante da lista dos heróis permanece idêntica abaixo...
                        InserirAgenteComSinergias("Kazumi", "Especialista", 5, 4, 4, 7, 13, "Hara-Kiri,Líder,Solo,Irmãs");
                        InserirAgenteComSinergias("Perdigas", "Suporte", 5, 2, 2, 8, 16, "Ordem,Fé");
                        InserirAgenteComSinergias("Maria Cecília", "Defensor", 5, 2, 16, 8, 2, "Ordem,Nova Ordem");
                        InserirAgenteComSinergias("Crane", "Lutador", 5, 17, 2, 7, 2, "Solo");
                        InserirAgenteComSinergias("Lilith", "Especialista", 5, 4, 4, 7, 13, "Vilão,Solo");
                        InserirAgenteComSinergias("Agni", "Suporte", 4, 2, 2, 6, 12, "A chama");
                        InserirAgenteComSinergias("Akane", "Lutador", 4, 13, 2, 5, 2, "Irmãs,Nova Ordem");
                        InserirAgenteComSinergias("Tom", "Lutador", 4, 13, 2, 5, 2, "Nova Ordem");
                        InserirAgenteComSinergias("Aziza", "Lutador", 4, 13, 2, 5, 2, "A chama");
                        InserirAgenteComSinergias("Shiva", "Especialista", 4, 3, 3, 5, 11, "Irmãos,Caos");
                        InserirAgenteComSinergias("Aya", "Suporte", 4, 2, 3, 7, 10, "Ninho do Dragão,Caos");
                        InserirAgenteComSinergias("Caim", "Defensor", 4, 2, 13, 6, 1, "Vilão");
                        InserirAgenteComSinergias("Viktor", "Defensor", 4, 2, 13, 6, 1, "Uagamora");
                        InserirAgenteComSinergias("Gabriel", "Especialista", 4, 2, 1, 6, 13, "Nova Ordem");
                        InserirAgenteComSinergias("Thoryn", "Defensor", 3, 1, 12, 5, 0, "Trindade,Fé");
                        InserirAgenteComSinergias("Nix", "Suporte", 3, 1, 1, 5, 11, "Fé");
                        InserirAgenteComSinergias("Zendaya", "Suporte", 3, 1, 1, 5, 11, "Trindade");
                        InserirAgenteComSinergias("Daerion", "Defensor", 3, 3, 7, 6, 2, "Casal Real,Amor platônico,Ninho do Dragão,Dragão");
                        InserirAgenteComSinergias("Shyvana", "Lutador", 3, 11, 1, 5, 1, "Uagamora,Dragão");
                        InserirAgenteComSinergias("Symon", "Suporte", 3, 1, 1, 5, 11, "Uagamora");
                        InserirAgenteComSinergias("Kael", "Lutador", 3, 11, 1, 5, 1, "Uagamora");
                        InserirAgenteComSinergias("Arkmeros", "Suporte", 3, 1, 1, 5, 11, "Fé");
                        InserirAgenteComSinergias("Ruivo", "Especialista", 3, 2, 0, 5, 11, "Vendedores,Ninho do Dragão");
                        InserirAgenteComSinergias("Marionetista", "Especialista", 3, 2, 0, 5, 11, "Vilão");
                        InserirAgenteComSinergias("Seph Flores", "Especialista", 3, 2, 0, 5, 11, "Hara-Kiri");
                        InserirAgenteComSinergias("Barbara", "Lutador", 3, 11, 1, 5, 1, "Trindade");
                        InserirAgenteComSinergias("Saphyra", "Lutador", 2, 9, 1, 3, 1, "Amor");
                        InserirAgenteComSinergias("Oriven", "Especialista", 2, 2, 0, 4, 8, "Uagamora");
                        InserirAgenteComSinergias("Megan", "Suporte", 2, 2, 0, 4, 8, "Hara-Kiri");
                        InserirAgenteComSinergias("Cael", "Lutador", 2, 8, 1, 4, 1, "Ninho do Dragão");
                        InserirAgenteComSinergias("Sirius", "Especialista", 2, 2, 0, 4, 8, "Caos");
                        InserirAgenteComSinergias("Marcos", "Defensor", 2, 1, 8, 4, 1, "Hara-Kiri");
                        InserirAgenteComSinergias("Shantal", "Suporte", 2, 1, 1, 4, 8, "A chama,Amor");
                        InserirAgenteComSinergias("Lamblin", "Lutador", 2, 9, 1, 3, 1, "Solo");
                        InserirAgenteComSinergias("Aryte", "Defensor", 2, 1, 8, 4, 1, "Uagamora");
                        InserirAgenteComSinergias("Zahra", "Defensor", 2, 1, 8, 4, 1, "Vendedores");
                        InserirAgenteComSinergias("Samir", "Lutador", 2, 6, 2, 3, 3, "Irmãos,Caos");
                        InserirAgenteComSinergias("Nyu", "Especialista", 2, 2, 0, 4, 8, "Hara-Kiri");
                        InserirAgenteComSinergias("Padre", "Suporte", 2, 1, 0, 4, 9, "Caos,Fé");
                        InserirAgenteComSinergias("Orion", "Suporte", 1, 1, 1, 3, 5, "Fé");
                        InserirAgenteComSinergias("Kru'el", "Especialista", 1, 1, 0, 3, 6, "Hara-Kiri");
                        InserirAgenteComSinergias("Meilyn", "Lutador", 1, 6, 1, 3, 0, "Amor platônico,Ninho do Dragão");
                        InserirAgenteComSinergias("Mauga", "Defensor", 1, 1, 6, 3, 0, "Uagamora");
                        InserirAgenteComSinergias("Marcus", "Defensor", 1, 1, 6, 3, 0, "Líder,Nova Ordem");
                        InserirAgenteComSinergias("Leão", "Especialista", 1, 1, 0, 3, 6, "Líder,Ordem");
                        InserirAgenteComSinergias("Matheus", "Lutador", 1, 6, 1, 3, 0, "Nova Ordem");
                        InserirAgenteComSinergias("Cronista", "Especialista", 1, 1, 0, 3, 6, "Uagamora");
                        InserirAgenteComSinergias("Vysenia", "Lutador", 1, 4, 2, 2, 2, "Casal Real,Líder,Ninho do Dragão,Dragão");
                        InserirAgenteComSinergias("Bree", "Suporte", 1, 1, 0, 3, 6, "Vendedores");
                        InserirAgenteComSinergias("Mimoso", "Lutador", 1, 6, 1, 3, 0, "Vendedores");
                        InserirAgenteComSinergias("Don Omar", "Especialista", 1, 1, 0, 3, 6, "Vendedores");
                        InserirAgenteComSinergias("Yasmin", "Suporte", 1, 1, 1, 3, 5, "Vendedores,Fé");
                        InserirAgenteComSinergias("Kyra", "Suporte", 1, 1, 1, 3, 5, "Uagamora");

                        await transaction.CommitAsync();
                    }
                }
            }
        }
    }
}