using Microsoft.Data.Sqlite;
using RPGBattleMaker.Data.Interface;

namespace RPGBattleMaker.Data
{
    public class DbContext : IDbContext
    {
        public DbContext()
        {
            
        }

        public async Task InitializeDatabase()
        {
            // O banco de dados será criado automaticamente na memória ou em um arquivo local na bin
            string connectionString = "Data Source=rpg_battle.db";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // 1. Cria a tabela se ela ainda não existir
                string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS Agentes (
                Agente TEXT, Tipo TEXT, Descricao TEXT, Player TEXT, 
                Raridade INTEGER, Sinergia TEXT, Ataque INTEGER, 
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
                INSERT INTO Agentes (Agente, Tipo, Descricao, Player, Raridade, Sinergia, Ataque, Defesa, Vida, Pericia) 
                VALUES ($agente, $tipo, $desc, $player, $raridade, $sinergia, $ataque, $defesa, $vida, $pericia);";

                    using (var insertCmd = new SqliteCommand(insertQuery, connection, transaction))
                    {
                        #region Heroes Insert
                        // Herói 1: Kazumi
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Kazumi");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "Líder da party, sniper habilidosa, boa em combate corpo a corpo. Fria e calculista.");
                        insertCmd.Parameters.AddWithValue("$player", "Wagner");
                        insertCmd.Parameters.AddWithValue("$raridade", 5);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Akane e Hara Kiri");
                        insertCmd.Parameters.AddWithValue("$ataque", 9);
                        insertCmd.Parameters.AddWithValue("$defesa", 6);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 10);
                        insertCmd.ExecuteNonQuery();

                        // Herói 2: Agni
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Agni");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "Maga, cega, fogo e maluca");
                        insertCmd.Parameters.AddWithValue("$player", "Wagner");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "A chama");
                        insertCmd.Parameters.AddWithValue("$ataque", 7);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 10);
                        insertCmd.ExecuteNonQuery();

                        // Herói 3: Akane
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Akane");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "Irmã da Kazumi, combate corpo a corpo");
                        insertCmd.Parameters.AddWithValue("$player", "Felipe");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Kazumi");
                        insertCmd.Parameters.AddWithValue("$ataque", 10);
                        insertCmd.Parameters.AddWithValue("$defesa", 5);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 4: Thoryn
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Thoryn");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$desc", "Paladino da justiça");
                        insertCmd.Parameters.AddWithValue("$player", "Wagner");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Zendaya");
                        insertCmd.Parameters.AddWithValue("$ataque", 3);
                        insertCmd.Parameters.AddWithValue("$defesa", 6);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 5: Saphyra
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Saphyra");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "Barbaro maluco");
                        insertCmd.Parameters.AddWithValue("$player", "Wagner");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Shantal");
                        insertCmd.Parameters.AddWithValue("$ataque", 6);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 6: Nix
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Nix");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "Clériga de cura maluca");
                        insertCmd.Parameters.AddWithValue("$player", "Wagner");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "");
                        insertCmd.Parameters.AddWithValue("$ataque", 0);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 9);
                        insertCmd.ExecuteNonQuery();

                        // Herói 7: Orion
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Orion");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "Clérigo defensor e racista");
                        insertCmd.Parameters.AddWithValue("$player", "Wagner");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 4);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 8: Zendaya
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Zendaya");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "Barda atiradora");
                        insertCmd.Parameters.AddWithValue("$player", "Ferreira");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Thoryn");
                        insertCmd.Parameters.AddWithValue("$ataque", 6);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();

                        // Herói 9: Perdigas
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Perdigas");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "Padre que enfrentou Lilith");
                        insertCmd.Parameters.AddWithValue("$player", "Ferreira");
                        insertCmd.Parameters.AddWithValue("$raridade", 5);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Maria Cecília e Leão");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 8);
                        insertCmd.Parameters.AddWithValue("$vida", 8);
                        insertCmd.Parameters.AddWithValue("$pericia", 10);
                        insertCmd.ExecuteNonQuery();

                        // Herói 10: Tom
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Tom");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "Ex-comandante do exército, velho habilidoso com armas de fogo");
                        insertCmd.Parameters.AddWithValue("$player", "Ferreira");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Maria Cecília");
                        insertCmd.Parameters.AddWithValue("$ataque", 9);
                        insertCmd.Parameters.AddWithValue("$defesa", 3);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 5);
                        insertCmd.ExecuteNonQuery();

                        // Herói 11: Oriven
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Oriven");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "Engenheiro e inventor habilidoso");
                        insertCmd.Parameters.AddWithValue("$player", "Ferreira");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis de Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 4);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 2);
                        insertCmd.Parameters.AddWithValue("$pericia", 7);
                        insertCmd.ExecuteNonQuery();

                        // Herói 12: Daerion
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Daerion");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$desc", "Inventor habilidoso para armaduras e montador de dragões");
                        insertCmd.Parameters.AddWithValue("$player", "Ferreira");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Dragão, Heróis do ninho");
                        insertCmd.Parameters.AddWithValue("$ataque", 0);
                        insertCmd.Parameters.AddWithValue("$defesa", 6);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();

                        // Herói 13: Kru'el
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Kru'el");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "Nomade, dirige carros e muito é um robo ligado a sexualidade");
                        insertCmd.Parameters.AddWithValue("$player", "Ferreira");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 1);
                        insertCmd.Parameters.AddWithValue("$pericia", 5);
                        insertCmd.ExecuteNonQuery();

                        // Herói 14: Maria Cecília
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Maria Cecília");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$desc", "Pistoleira veterana e que se ligou a morte, se tornando quase uma anja da morte");
                        insertCmd.Parameters.AddWithValue("$player", "Laura");
                        insertCmd.Parameters.AddWithValue("$raridade", 5);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Perdigas, Tom");
                        insertCmd.Parameters.AddWithValue("$ataque", 8);
                        insertCmd.Parameters.AddWithValue("$defesa", 6);
                        insertCmd.Parameters.AddWithValue("$vida", 6);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();

                        // Herói 15: Meilyn
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Meilyn");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "Samurai asiática");
                        insertCmd.Parameters.AddWithValue("$player", "Laura");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis do Ninho");
                        insertCmd.Parameters.AddWithValue("$ataque", 5);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 16: Megan
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Megan");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "Médica de combate em ambiente cyberpunk");
                        insertCmd.Parameters.AddWithValue("$player", "Laura");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Hara Kiri");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 2);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();

                        // Herói 17: Crane
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Crane");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "Guerreiro clássico e mortal");
                        insertCmd.Parameters.AddWithValue("$player", "Felipe");
                        insertCmd.Parameters.AddWithValue("$raridade", 5);
                        insertCmd.Parameters.AddWithValue("$sinergia", "");
                        insertCmd.Parameters.AddWithValue("$ataque", 10);
                        insertCmd.Parameters.AddWithValue("$defesa", 10);
                        insertCmd.Parameters.AddWithValue("$vida", 8);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 18: Aziza
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Aziza");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "Bruxa hexblade mortal");
                        insertCmd.Parameters.AddWithValue("$player", "Felipe");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "A chama");
                        insertCmd.Parameters.AddWithValue("$ataque", 10);
                        insertCmd.Parameters.AddWithValue("$defesa", 5);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 3);
                        insertCmd.ExecuteNonQuery();

                        // Herói 19: Cael
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Cael");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "Ranger e arqueiro inteligente");
                        insertCmd.Parameters.AddWithValue("$player", "Felipe");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis do Ninho");
                        insertCmd.Parameters.AddWithValue("$ataque", 5);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 4);
                        insertCmd.ExecuteNonQuery();

                        // Herói 20: Mauga
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Mauga");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$desc", "Dobrador de metal e ar");
                        insertCmd.Parameters.AddWithValue("$player", "Felipe");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis de Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 5);
                        insertCmd.Parameters.AddWithValue("$vida", 2);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 21: Shyvana
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Shyvana");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "Barbará meio dragão");
                        insertCmd.Parameters.AddWithValue("$player", "Felipe");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis de Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 7);
                        insertCmd.Parameters.AddWithValue("$defesa", 3);
                        insertCmd.Parameters.AddWithValue("$vida", 6);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 22: Shyva
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Shyva");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "Maga habilidosa com lâminas e feitiços");
                        insertCmd.Parameters.AddWithValue("$player", "Felipe");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Ao inferno");
                        insertCmd.Parameters.AddWithValue("$ataque", 7);
                        insertCmd.Parameters.AddWithValue("$defesa", 5);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 7);
                        insertCmd.ExecuteNonQuery();

                        // Herói 23: Marcus
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Marcus");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$desc", "grandão líder e de coração igrande");
                        insertCmd.Parameters.AddWithValue("$player", "Felipe");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 4);
                        insertCmd.Parameters.AddWithValue("$vida", 2);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 24: Leão
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Leão");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "polícial bom com armas e investigação");
                        insertCmd.Parameters.AddWithValue("$player", "Felipe");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Maria Cecilia e Perdigas");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 2);
                        insertCmd.Parameters.AddWithValue("$pericia", 4);
                        insertCmd.ExecuteNonQuery();

                        // Herói 25: Matheus
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Matheus");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "bom combatente");
                        insertCmd.Parameters.AddWithValue("$player", "Jardel");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "");
                        insertCmd.Parameters.AddWithValue("$ataque", 4);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 26: Sirius
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Sirius");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "invoca um parceiro gigante para ajudar na luta");
                        insertCmd.Parameters.AddWithValue("$player", "Jardel");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Ao inferno");
                        insertCmd.Parameters.AddWithValue("$ataque", 3);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 5);
                        insertCmd.ExecuteNonQuery();

                        // Herói 27: Aya
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Aya");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "barda extremamente poderosa");
                        insertCmd.Parameters.AddWithValue("$player", "Jardel");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis do Ninho");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 3);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 12);
                        insertCmd.ExecuteNonQuery();

                        // Herói 28: Marcos
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Marcos");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "grande combatente corpo a corpo em cyberpunk - muita vida");
                        insertCmd.Parameters.AddWithValue("$player", "Jardel");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Hara Kiri");
                        insertCmd.Parameters.AddWithValue("$ataque", 5);
                        insertCmd.Parameters.AddWithValue("$defesa", 4);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 29: Symon
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Symon");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "druida de cura e controle");
                        insertCmd.Parameters.AddWithValue("$player", "Jardel");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis de Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 3);
                        insertCmd.Parameters.AddWithValue("$defesa", 3);
                        insertCmd.Parameters.AddWithValue("$vida", 6);
                        insertCmd.Parameters.AddWithValue("$pericia", 6);
                        insertCmd.ExecuteNonQuery();

                        // Herói 30: Shantal
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Shantal");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "barda que desceu ao abismo para manter a chama acesa");
                        insertCmd.Parameters.AddWithValue("$player", "Laura");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "A chama");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 7);
                        insertCmd.ExecuteNonQuery();

                        // Herói 31: Lilith
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Lilith");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "Grande vilã da campanha de ordem paranormal, a invocação do sangue");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 5);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vilão");
                        insertCmd.Parameters.AddWithValue("$ataque", 6);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 8);
                        insertCmd.Parameters.AddWithValue("$pericia", 12);
                        insertCmd.ExecuteNonQuery();

                        // Herói 32: Caim
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Caim");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "Grande vilão da campanha de ordem, a invocação da loucura");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vilão");
                        insertCmd.Parameters.AddWithValue("$ataque", 8);
                        insertCmd.Parameters.AddWithValue("$defesa", 5);
                        insertCmd.Parameters.AddWithValue("$vida", 8);
                        insertCmd.Parameters.AddWithValue("$pericia", 1);
                        insertCmd.ExecuteNonQuery();

                        // Herói 33: Lamblin
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Lamblin");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "arqueiro elfo primoroso");
                        insertCmd.Parameters.AddWithValue("$player", "Ramon");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "");
                        insertCmd.Parameters.AddWithValue("$ataque", 5);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 2);
                        insertCmd.Parameters.AddWithValue("$pericia", 5);
                        insertCmd.ExecuteNonQuery();

                        // Herói 34: Kael
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Kael");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "guerreiro orc que está fazendo seu nome");
                        insertCmd.Parameters.AddWithValue("$player", "Ramon");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis de Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 12);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 5);
                        insertCmd.Parameters.AddWithValue("$pericia", 0);
                        insertCmd.ExecuteNonQuery();

                        // Herói 35: Arkmeros
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Arkmeros");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "tiefling clerigo leal e bom");
                        insertCmd.Parameters.AddWithValue("$player", "Bruno");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 5);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 9);
                        insertCmd.ExecuteNonQuery();

                        // Herói 36: Ruivo
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Ruivo");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "feiticeiro que se tornou um comerciante");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedor");
                        insertCmd.Parameters.AddWithValue("$ataque", 6);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 7);
                        insertCmd.ExecuteNonQuery();

                        // Herói 37: Viktor
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Viktor");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$desc", "guardião que usa seu escudo para proteger seus companheiros");
                        insertCmd.Parameters.AddWithValue("$player", "Bruno");
                        insertCmd.Parameters.AddWithValue("$raridade", 4);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis de Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 0);
                        insertCmd.Parameters.AddWithValue("$defesa", 12);
                        insertCmd.Parameters.AddWithValue("$vida", 8);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 38: Cronista
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Cronista");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "grande conhecedor da história do mundo");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "");
                        insertCmd.Parameters.AddWithValue("$ataque", 3);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 3);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();

                        // Herói 39: Marionetista
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Marionetista");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "Vilão baseado no sasori desta campanha");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 3);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vilão");
                        insertCmd.Parameters.AddWithValue("$ataque", 7);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 2);
                        insertCmd.Parameters.AddWithValue("$pericia", 7);
                        insertCmd.ExecuteNonQuery();

                        // Herói 40: Aryte
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Aryte");
                        insertCmd.Parameters.AddWithValue("$tipo", "Defensor");
                        insertCmd.Parameters.AddWithValue("$desc", "guerreiro lagarto que ajuda o time");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis de Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 5);
                        insertCmd.Parameters.AddWithValue("$defesa", 5);
                        insertCmd.Parameters.AddWithValue("$vida", 2);
                        insertCmd.Parameters.AddWithValue("$pericia", 2);
                        insertCmd.ExecuteNonQuery();

                        // Herói 41: Vysenia
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Vysenia");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "rainha de um grande império e montadora de dragões");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Dragão");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 1);
                        insertCmd.Parameters.AddWithValue("$pericia", 6);
                        insertCmd.ExecuteNonQuery();

                        // Herói 42: Bree
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Bree");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "aprendiz de maga e vendora de itens");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedor");
                        insertCmd.Parameters.AddWithValue("$ataque", 2);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 1);
                        insertCmd.Parameters.AddWithValue("$pericia", 5);
                        insertCmd.ExecuteNonQuery();

                        // Herói 43: Mimoso
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Mimoso");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "ferreiro gato que era um grande aventureiro antes");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedor");
                        insertCmd.Parameters.AddWithValue("$ataque", 3);
                        insertCmd.Parameters.AddWithValue("$defesa", 2);
                        insertCmd.Parameters.AddWithValue("$vida", 2);
                        insertCmd.Parameters.AddWithValue("$pericia", 3);
                        insertCmd.ExecuteNonQuery();

                        // Herói 44: Don Omar
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Don Omar");
                        insertCmd.Parameters.AddWithValue("$tipo", "Especialista");
                        insertCmd.Parameters.AddWithValue("$desc", "velho que é muito inteligente e curioso");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 1);
                        insertCmd.Parameters.AddWithValue("$vida", 1);
                        insertCmd.Parameters.AddWithValue("$pericia", 7);
                        insertCmd.ExecuteNonQuery();

                        // Herói 45: Yasmin
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Yasmin");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "clériga e médica do acampamento");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedor");
                        insertCmd.Parameters.AddWithValue("$ataque", 0);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 4);
                        insertCmd.Parameters.AddWithValue("$pericia", 6);
                        insertCmd.ExecuteNonQuery();

                        // Herói 46: Zahra
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Zahra");
                        insertCmd.Parameters.AddWithValue("$tipo", "Lutador");
                        insertCmd.Parameters.AddWithValue("$desc", "maga de combate do império");
                        insertCmd.Parameters.AddWithValue("$player", "NPC");
                        insertCmd.Parameters.AddWithValue("$raridade", 2);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Vendedor");
                        insertCmd.Parameters.AddWithValue("$ataque", 7);
                        insertCmd.Parameters.AddWithValue("$defesa", 3);
                        insertCmd.Parameters.AddWithValue("$vida", 1);
                        insertCmd.Parameters.AddWithValue("$pericia", 3);
                        insertCmd.ExecuteNonQuery();

                        // Herói 47: Kyra
                        insertCmd.Parameters.Clear();
                        insertCmd.Parameters.AddWithValue("$agente", "Kyra");
                        insertCmd.Parameters.AddWithValue("$tipo", "Suporte");
                        insertCmd.Parameters.AddWithValue("$desc", "barda de buff, mas não se mostrou muito");
                        insertCmd.Parameters.AddWithValue("$player", "Laura");
                        insertCmd.Parameters.AddWithValue("$raridade", 1);
                        insertCmd.Parameters.AddWithValue("$sinergia", "Heróis de Uagamora");
                        insertCmd.Parameters.AddWithValue("$ataque", 1);
                        insertCmd.Parameters.AddWithValue("$defesa", 0);
                        insertCmd.Parameters.AddWithValue("$vida", 1);
                        insertCmd.Parameters.AddWithValue("$pericia", 8);
                        insertCmd.ExecuteNonQuery();
                        #endregion

                        transaction.Commit();
                    }
                }
            }
        }
    }
}
