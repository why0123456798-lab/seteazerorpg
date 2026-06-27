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

                string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS Agentes (
                Agente TEXT, Tipo TEXT, 
                Raridade INTEGER, Sinergias TEXT, Ataque INTEGER, 
                Defesa INTEGER, Vida INTEGER, Pericia INTEGER
            );";

                using (var createTableCmd = new SqliteCommand(createTableQuery, connection))
                {
                    createTableCmd.ExecuteNonQuery();
                }

                // 2. RESETA O BANCO: Limpa todos os dados anteriores para garantir o estado original
                using (var deleteCmd = new SqliteCommand("DELETE FROM Agentes;", connection))
                {
                    deleteCmd.ExecuteNonQuery();
                }

                // 3. SEED: Insere os valores originais e imutáveis direto via código
                // Use uma transação para fazer as inserções de forma incrivelmente rápida
                using (var transaction = connection.BeginTransaction())
                {
                    string insertQuery = @"
                INSERT INTO Agentes (Agente, Tipo, Raridade, Sinergias, Ataque, Defesa, Vida, Pericia) 
                VALUES ($agente, $tipo, $raridade, $sinergia, $ataque, $defesa, $vida, $pericia);";

                    using (var insertCmd = new SqliteCommand(insertQuery, connection, transaction))
                    {
                        #region Heroes Insert
                        // Herói 1: Kazumi
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Kazumi");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 5);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Hara-Kiri,Líder,Solo");
                        insertCmd.Parameters.AddWithValue("$ataque", 4);
                        insertCmd.Parameters.AddWithValue("$defesa", 4);
                        insertCmd.Parameters.AddWithValue("$vida", 7);
                        insertCmd.Parameters.AddWithValue("$pericia", 13);
                        insertCmd.ExecuteNonQuery();

                        // Herói 2: Perdigas
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Perdigas");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 5);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Ordem,Fé");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 8);
                        insertCmd.Parameters.AddWithValue("$pericia", 16);
                        insertCmd.ExecuteNonQuery();

                        // Herói 3: Maria Cecília
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Maria Cecília");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$raridade", 5);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Ordem,Nova Ordem");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 16);
                        insertCmd.Parameters.AddWithValue("$vida", 8);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 4: Crane
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Crane");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 5);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Solo");
                        insertCmd.Parameters.AddWithValue("$ataque", 17);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 7);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 5: Lilith
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Lilith");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 5);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vilão,Solo");
                        insertCmd.Parameters.AddWithValue("$ataque", 4);
                        insertCmd.Parameters.AddWithValue("$defesa", 4);
                        insertCmd.Parameters.AddWithValue("$vida", 7);
                        insertCmd.Parameters.AddWithValue("$pericia", 13);
                        insertCmd.ExecuteNonQuery();

                        // Herói 6: Agni
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Agni");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "A chama");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 6);
                        insertCmd.Parameters.AddWithValue("$pericia", 12);
                        insertCmd.ExecuteNonQuery();

                        // Herói 7: Akane
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Akane");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Irmãs,Nova Ordem");
                        insertCmd.Parameters.AddWithValue("$ataque", 13);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 8: Tom
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Tom");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Nova Ordem");
                        insertCmd.Parameters.AddWithValue("$ataque", 13);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 9: Aziza
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Aziza");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "A chama");
                        insertCmd.Parameters.AddWithValue("$ataque", 13);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 10: Shyva
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Shyva");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Irmãos,Caos");
                        insertCmd.Parameters.AddWithValue("$ataque", 3);
                        insertCmd.Parameters.AddWithValue("$defesa", 3);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 11);
                        insertCmd.ExecuteNonQuery();

                        // Herói 11: Aya
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Aya");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Trindade,Ninho do Dragão,Caos");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 3);
                        insertCmd.Parameters.AddWithValue("$vida", 7);
                        insertCmd.Parameters.AddWithValue("$pericia", 10);
                        insertCmd.ExecuteNonQuery();

                        // Herói 12: Caim
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Caim");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vilão");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 13);
                        insertCmd.Parameters.AddWithValue("$vida", 6);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 13: Viktor
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Viktor");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 13);
                        insertCmd.Parameters.AddWithValue("$vida", 6);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 14: Gabriel
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Gabriel");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Nova Ordem");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 6);
                        insertCmd.Parameters.AddWithValue("$pericia", 13);
                        insertCmd.ExecuteNonQuery();

                        // Herói 15: Thoryn
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Thoryn");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Trindade,Fé");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 12);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 16: Nix
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Nix");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Fé");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 11);
                        insertCmd.ExecuteNonQuery();

                        // Herói 17: Zendaya
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Zendaya");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Trindade");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 11);
                        insertCmd.ExecuteNonQuery();

                        // Herói 18: Daerion
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Daerion");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Casal Real,Amor platônico,Ninho do Dragão,Dragão");
                        insertCmd.Parameters.AddWithValue("$ataque", 3);
                        insertCmd.Parameters.AddWithValue("$defesa", 7);
                        insertCmd.Parameters.AddWithValue("$vida", 6);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 19: Shyvana
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Shyvana");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Uagamora,Dragão");
                        insertCmd.Parameters.AddWithValue("$ataque", 11);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 20: Symon
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Symon");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 11);
                        insertCmd.ExecuteNonQuery();

                        // Herói 21: Kael
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Kael");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 11);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 22: Arkmeros
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Arkmeros");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Fé");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 11);
                        insertCmd.ExecuteNonQuery();

                        // Herói 23: Ruivo
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Ruivo");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedores");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 11);
                        insertCmd.ExecuteNonQuery();

                        // Herói 24: Marionetista
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Marionetista");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vilão");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 11);
                        insertCmd.ExecuteNonQuery();

                        // Herói 25: Seph Flores
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Seph Flores");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Hara-Kiri");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 11);
                        insertCmd.ExecuteNonQuery();

                        // Herói 26: Barbara
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Barbara");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Trindade");
                        insertCmd.Parameters.AddWithValue("$ataque", 11);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 27: Saphyra
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Saphyra");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Amor");
                        insertCmd.Parameters.AddWithValue("$ataque", 9);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 28: Oriven
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Oriven");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();

                        // Herói 29: Megan
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Megan");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Hara-Kiri");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();

                        // Herói 30: Cael
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Cael");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Ninho do Dragão");
                        insertCmd.Parameters.AddWithValue("$ataque", 8);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 31: Sirius
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Sirius");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Caos");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();

                        // Herói 32: Marcos
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Marcos");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Hara-Kiri");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 8);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 33: Shantal
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Shantal");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "A chama,Amor");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();

                        // Herói 34: Lamblin
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Lamblin");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Solo");
                        insertCmd.Parameters.AddWithValue("$ataque", 9);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 35: Aryte
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Aryte");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 8);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 36: Zahra
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Zahra");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedores");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 8);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 37: Samir
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Samir");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Irmãos,Caos");
                        insertCmd.Parameters.AddWithValue("$ataque", 6);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 3);
                        insertCmd.ExecuteNonQuery();

                        // Herói 38: Nyu
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Nyu");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Hara-Kiri");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();

                        // Herói 39: Padre
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Padre");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Caos,Fé");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 9);
                        insertCmd.ExecuteNonQuery();

                        // Herói 40: Orion
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Orion");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Fé");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 5);
                        insertCmd.ExecuteNonQuery();

                        // Herói 41: Kru'el
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Kru'el");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Hara-Kiri");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 6);
                        insertCmd.ExecuteNonQuery();

                        // Herói 42: Meilyn
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Meilyn");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Amor platônico,Ninho do Dragão");
                        insertCmd.Parameters.AddWithValue("$ataque", 6);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 43: Mauga
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Mauga");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 6);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 44: Marcus
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Marcus");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Líder,Nova Ordem");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 6);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 45: Leão
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Leão");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Líder,Ordem");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 6);
                        insertCmd.ExecuteNonQuery();

                        // Herói 46: Matheus
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Matheus");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Nova Ordem");
                        insertCmd.Parameters.AddWithValue("$ataque", 6);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 47: Cronista
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Cronista");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 6);
                        insertCmd.ExecuteNonQuery();

                        // Herói 48: Vysenia
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Vysenia");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Casal Real,Líder,Ninho do Dragão,Dragão");
                        insertCmd.Parameters.AddWithValue("$ataque", 4);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 2);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 49: Bree
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Bree");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedores");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 6);
                        insertCmd.ExecuteNonQuery();

                        // Herói 50: Mimoso
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Mimoso");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedores");
                        insertCmd.Parameters.AddWithValue("$ataque", 6);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 51: Don Omar
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Don Omar");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedores");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 6);
                        insertCmd.ExecuteNonQuery();

                        // Herói 52: Yasmin
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Yasmin");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedores,Fé");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 5);
                        insertCmd.ExecuteNonQuery();

                        // Herói 53: Kyra
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Kyra");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 5);
                        insertCmd.ExecuteNonQuery();
                        #endregion

                        transaction.Commit();
                    }
                }
            }
        }
    }
}
