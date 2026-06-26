import pandas as pd
import random
import time

class Agent:
    def __init__(self, row):
        self.name = row['Agente']
        self.type = row['Tipo']
        self.desc = row['Descrição']
        self.player = row['Player']
        self.rarity = int(row['Raridade'])
        self.synergy_text = str(row['Sinergia']).strip()
        
        # Atributos base vindos do CSV
        self.base_attack = int(row['Ataque'])
        self.base_defense = int(row['Defesa'])
        self.max_life = int(row['Vida'])
        self.base_skill = int(row['Perícia'])
        
        # Status dinâmicos na partida
        self.current_life = self.max_life
        self.fatigue = {"Ataque": 0, "Defesa": 0, "Perícia": 0}

    def reset_status(self):
        self.current_life = self.max_life
        self.fatigue = {"Ataque": 0, "Defesa": 0, "Perícia": 0}

    def reset_fatigue(self):
        self.fatigue = {"Ataque": 0, "Defesa": 0, "Perícia": 0}

    def get_attr(self, attr_name, team_agents):
        # Retorna o atributo considerando base, fadiga e sinergias
        base = 0
        if attr_name == "Ataque": base = self.base_attack
        elif attr_name == "Defesa": base = self.base_defense
        elif attr_name == "Perícia": base = self.base_skill
        
        val = base - self.fatigue.get(attr_name, 0)
        val += self.get_synergy_bonus(team_agents)
        return max(0, val)

    def get_synergy_bonus(self, team_agents):
        bonus = 0
        names_in_team = [a.name for a in team_agents]
        
        # 1. Duplas específicas
        pairs = [
            ("Kazumi", "Akane"), ("Thoryn", "Zendaya"), 
            ("Perdigas", "Maria Cecília"), ("Tom", "Maria Cecília"), 
            ("Saphyra", "Shantal")
        ]
        for p1, p2 in pairs:
            if self.name in (p1, p2):
                if p1 in names_in_team and p2 in names_in_team:
                    bonus += 3

        # 2. Grupos / Facções
        groups = ["Heróis de Uagamora", "Heróis do Ninho", "Hara Kiri", "A Chama", "A Banda", "Vilões", "Vendedor", "Dragão"]
        for g in groups:
            # Verifica se o agente pertence àquela sinergia textualmente
            if g.lower() in self.synergy_text.lower() or (g == "Vilões" and "vilão" in self.synergy_text.lower()):
                # Conta quantos no time pertencem ao mesmo grupo
                count = 0
                for a in team_agents:
                    if g.lower() in a.synergy_text.lower() or (g == "Vilões" and "vilão" in a.synergy_text.lower()):
                        count += 1
                if count == 3: bonus += 2
                elif count == 4: bonus += 3
                elif count >= 5: bonus += 4
        return bonus


class Game:
    def __init__(self, csv_path):
        import os
        base_dir = os.path.dirname(os.path.abspath(__file__))
        full_path = os.path.join(base_dir, csv_path)
        
        try:
            self.df = pd.read_csv(full_path)
        except FileNotFoundError:
            print("\n" + "!"*60)
            print(f"❌ ERRO: O arquivo '{csv_path}' não foi encontrado!")
            print(f"Procurei no caminho: {full_path}")
            print("Verifique se o arquivo .csv está na MESMA pasta do script game.py.")
            print("!"*60 + "\n")
            raise
            
        self.all_agents = [Agent(row) for _, row in self.df.iterrows()]
        self.reset_game_state()

    def reset_game_state(self):
        # Reseta os heróis do banco de dados para os valores máximos originais
        for a in self.all_agents:
            a.reset_status()
        self.team = []
        self.gold = 10
        self.current_level = 1
        self.mode = "Fácil"
        self.round_bonuses = {"Ataque": 0, "Defesa": 0, "Escudo": 0, "DC_Reduction": 0}

    def choose_mode(self):
        print("="*50)
        print("      BEM-VINDO AO RPG AUTOBATTLER ROGUELIKE      ")
        print("="*50)
        self.mode = "Difícil"

    def get_market_pool(self):
        # Filtra os agentes que já não estão no time do jogador
        team_names = [a.name for a in self.team]
        return [a for a in self.all_agents if a.name not in team_names]

    def roll_market(self):
        pool = self.get_market_pool()
        if not pool:
            return []
        
        market_slots = []
        # Pesos de probabilidade para cada Raridade (Custo)
        # Custo:  1    2    3    4    5
        weights = [0.40, 0.30, 0.15, 0.10, 0.05]
        
        while len(market_slots) < 4:
            chosen_rarity = random.choices([1, 2, 3, 4, 5], weights=weights, k=1)[0]
            valid_options = [a for a in pool if a.rarity == chosen_rarity and a not in market_slots]
            
            # Mecânica de Segurança: Se faltar herói da raridade sorteada, tenta rebaixar
            while not valid_options and chosen_rarity > 1:
                chosen_rarity -= 1
                valid_options = [a for a in pool if a.rarity == chosen_rarity and a not in market_slots]
            
            if valid_options:
                market_slots.append(random.choice(valid_options))
            else:
                # Se mesmo assim esgotar completamente as opções válidas
                remaining = [a for a in pool if a not in market_slots]
                if remaining:
                    market_slots.append(random.choice(remaining))
                else:
                    break
        return market_slots

    def show_team(self):
        print("\n--- SEU TIME ATUAL ---")
        if not self.team:
            print("(Nenhum agente escalado)")
        for i, a in enumerate(self.team):
            escudo_str = f" (+ Escudo)" if self.round_bonuses["Escudo"] > 0 else "" # Será calculado por fase
            print(f"[{i+1}] {a.name} ({a.type}) - Custo {a.rarity} | ❤️ Vida: {a.current_life}/{a.max_life} | ⚔️ ATK:{a.base_attack} 🛡️ DEF:{a.base_defense} 🎯 PER:{a.base_skill}")
        print("-" * 30)

    def shop_phase(self):
        market = self.roll_market()
        while True:
            self.show_team()
            print(f"💰 Moedas Disponíveis: {self.gold}g | Missão Atual: Nível {self.current_level}")
            
            print("\n--- MERCADO DO TURNO ---")
            for i, a in enumerate(market):
                if a is None:
                    print(f"[{i+1}] --------- COMPRADO ---------")
                else:
                    print(f"[{i+1}] 🪙   {a.rarity}g - {a.name} ({a.type}) | \"{a.desc[:45]}...\" | ATK:{a.base_attack} DEF:{a.base_defense} PER:{a.base_skill} VID:{a.max_life}")
            
            print("\n[R] Reroll Loja (1g)  [V] Vender Herói  [M] Ir para Missão")
            choice = input("Escolha uma ação ou número para comprar: ").strip().upper()

            if choice == 'R':
                if self.gold >= 1:
                    self.gold -= 1
                    market = self.roll_market()
                    print("\nLoja atualizada!")
                else:
                    print("\nMoedas insuficientes para Reroll!")
            elif choice == 'V':
                if not self.team:
                    print("\nTime vazio.")
                    continue
                self.show_team()
                idx = input("Digite o número do herói para vender: ")
                if idx.isdigit() and 1 <= int(idx) <= len(self.team):
                    removed = self.team.pop(int(idx)-1)
                    self.gold += removed.rarity
                    print(f"\n{removed.name} vendido por {removed.rarity}g!")
                else:
                    print("\nOpção inválida.")
            elif choice == 'M':
                # --- VERIFICAÇÃO DE FALÊNCIA ECONÔMICA ---
                pode_dar_reroll = (self.gold >= 1)
                
                # Verifica se há pelo menos um herói acessível no mercado atual
                pode_comprar_algo = False
                for a in market:
                    if a is not None and self.gold >= a.rarity:
                        pode_comprar_algo = True
                        break
                
                # Se não tem dinheiro pra reroll E não consegue comprar ninguém da vitrine:
                esta_travado = (not pode_dar_reroll) and (not pode_comprar_algo)
                if len(self.team) > 5:
                    print("\nErro: Sua equipe excede o limite máximo de 5 personagens!")
                elif len(self.team) == 0:
                    print("\nErro: Você não pode ir para a missão com o time completamente vazio (0 heróis)!")
                else:
                    if esta_travado and len(self.team) < 3:
                        print("\n⚠️ EXCEÇÃO DE RECURSOS: Detectamos que você está sem moedas para ações na loja.")
                        print(f"Avançando para a missão com apenas {len(self.team)} herói(s). Boa sorte!")
                        input("[Pressione Enter para continuar]")
                    break
            elif choice.isdigit() and 1 <= int(choice) <= len(market):
                chosen_agent = market[int(choice)-1]
                
                if chosen_agent is None:
                    print("\nEste personagem já foi comprado!")
                    continue
                    
                if self.gold >= chosen_agent.rarity:
                    if len(self.team) < 5:
                        self.gold -= chosen_agent.rarity
                        self.team.append(chosen_agent)
                        market[int(choice)-1] = None  
                        print(f"\n{chosen_agent.name} comprado com sucesso!")
                    else:
                        print("\nEquipe cheia (máximo 5 personagens)!")
                else:
                    print("\nMoedas insuficientes!")
            else:
                print("\nComando inválido.")

    def run_mission(self):
        themes = ["Ataque", "Defesa", "Perícia"]
        current_theme = random.choice(themes)
        
        dc_table = {1: 10, 2: 13, 3: 16, 4: 20, 5: 25}
        base_dc = dc_table[self.current_level]
        dc = max(1, base_dc - self.round_bonuses["DC_Reduction"])
        
        print("\n" + "="*50)
        print(f"🚨 INICIANDO MISSÃO NÍVEL {self.current_level} | TEMA: {current_theme.upper()} 🚨")
        print(f"Dificuldade do Andar (DC): {dc} (DC Base: {base_dc})")
        print("="*50)
        time.sleep(1)

        shields = {a.name: self.round_bonuses["Escudo"] for a in self.team}
        sucessos = 0
        falhas = 0  # Nova variável para contar as falhas acumuladas
        
        for teste_num in range(1, 6):
            alive_heroes = [a for a in self.team if a.current_life > 0]
            if not alive_heroes:
                print("\n☠️ Todos os heróis morreram no meio da missão!")
                falhas = 3  # Força o Game Over por falta de combatentes
                break
                
            best_hero = None
            best_val = -999
            
            for a in alive_heroes:
                val = a.get_attr(current_theme, self.team)
                if current_theme == "Ataque":
                    val += self.round_bonuses["Ataque"]
                elif current_theme == "Defesa":
                    val += self.round_bonuses["Defesa"]
                
                if val > best_val:
                    best_val = val
                    best_hero = a
            
            print(f"\n👉 Teste {teste_num}/5: {best_hero.name} assume a liderança com {current_theme} total de {best_val}.")
            input("[Pressione Enter para rolar o d20]")
            
            d20 = random.randint(1, 20)
            total = best_val + d20
            print(f"🎲 Rolagem: {d20} (Dado) + {best_val} (Atributo) = {total} vs DC {dc}")
            
            if self.mode == "Difícil" and d20 == 20:
                print("🌟 CRÍTICO POSITIVO (20 NATURAL)! Contando como 2 SUCESSOS!")
                sucessos += 2
            elif self.mode == "Difícil" and d20 == 1:
                print("💀 CRÍTICO NEGATIVO (1 NATURAL)! Contando como 1 FALHA CRÍTICA!")
                falhas += 2
                dano = self.current_level * 2
                self.apply_damage(best_hero, dano, shields)
            elif total >= dc:
                print("🟢 SUCESSO!")
                sucessos += 1
            else:
                print("🔴 FALHA!")
                falhas += 1
                dano = self.current_level
                self.apply_damage(best_hero, dano, shields)
                
            # CHECAGEM DE GAME OVER IMEDIATO
            if falhas >= 3:
                print(f"\n❌ O time acumulou {falhas} falhas nesta missão!")
                break
            best_hero.fatigue[current_theme] += 1
            print(f"💤 {best_hero.name} fadigou em {current_theme} (Fadiga Atual: -{best_hero.fatigue[current_theme]}).")
            time.sleep(1)

        print("\n" + "="*40)
        print(f"FIM DOS TESTES. PLACAR: {sucessos} SUCESSOS | {falhas} FALHAS")
        print("="*40)
        
        # Limpa as fadigadas após a missão terminar ou falhar
        for a in self.team:
            a.reset_fatigue()
            
        if falhas >= 3:
            print("\n🔴 GAME OVER! Sua equipe acumulou 3 ou mais falhas e não conseguiu completar o andar.")
            return False
        elif sucessos == 3:
            print("\n🟡 VITÓRIA PÍRRICA! Vocês avançam, mas por um triz. (+2 Moedas)")
            self.gold += 2
        elif sucessos == 4:
            print("\n🔵 VITÓRIA CONFIANTE! Uma excelente exibição de estratégia. (+5 Moedas)")
            self.gold += 5
        elif sucessos >= 5:
            print("\n🟢 VITÓRIA ABSOLUTA! Perfeito e lendário! (+8 Moedas)")
            self.gold += 8
            
        self.run_rest_phase()
        self.current_level += 1
        return True

    def apply_damage(self, hero, dano, shields):
        if shields[hero.name] > 0:
            if shields[hero.name] >= dano:
                shields[hero.name] -= dano
                print(f"🛡️ O escudo absorveu todo o dano! Escudo restante em {hero.name}: {shields[hero.name]}")
                return
            else:
                dano -= shields[hero.name]
                print(f"🛡️ O escudo quebrou absorvendo parte do golpe! {hero.name} vai tomar {dano} de dano direto.")
                shields[hero.name] = 0
                
        hero.current_life -= dano
        print(f"💥 {hero.name} recebeu {dano} de dano! Vida restante: {max(0, hero.current_life)}/{hero.max_life}")
        
        if hero.current_life <= 0:
            if self.mode == "Difícil":
                print(f"☠️ PERMADEATH: {hero.name} morreu definitivamente e foi apagado do time!")
                self.team.remove(hero)
            else:
                hero.current_life = 0
                print(f"💤 NOCAUTE: {hero.name} desmaiou e está incapacitado até o fim da missão!")

    def run_rest_phase(self):
        print("\n☕ FASE DE DESCANSO: Contabilizando Habilidades de Classe...")
        self.gold += 5 # Salário fixo
        
        # Conta a quantidade de cada classe presentes no time
        suportes = min(3, sum(1 for a in self.team if a.type == "Suporte" and a.current_life > 0))
        defensores = min(3, sum(1 for a in self.team if a.type == "Defensor" and a.current_life > 0))
        lutadores = min(3, sum(1 for a in self.team if a.type == "Lutador" and a.current_life > 0))
        especialistas = min(3, sum(1 for a in self.team if a.type == "Especialista" and a.current_life > 0))
        
        # Limpa nocautes no modo Fácil
        if self.mode == "Fácil":
            for a in self.team:
                if a.current_life == 0:
                    a.current_life = 1
                    print(f"❤️ {a.name} acordou do nocaute com 1 de vida.")

        # 1. Aplica cura dos Suportes
        if suportes > 0:
            for a in self.team:
                if a.current_life > 0:
                    a.current_life = min(a.max_life, a.current_life + suportes)
            print(f"💚 {suportes} Suporte(s) ativo(s): Todos os aliados recuperaram +{suportes} de Vida.")
            
        # 2. Configura bônus de Defensores (Escudos) para a próxima missão
        self.round_bonuses["Escudo"] = defensores
        if defensores > 0:
            print(f"🛡️ {defensores} Defensor(es) ativo(s): Equipe começará com +{defensores} de Escudo temporário.")
            
        # 3. Configura bônus de Lutadores para a próxima missão
        self.round_bonuses["Ataque"] = lutadores
        self.round_bonuses["Defesa"] = lutadores
        if lutadores > 0:
            print(f"🔥 {lutadores} Lutador(es) ativo(s): +{lutadores} de bônus no resultado geral dos dados na próxima fase.")
            
        # 4. Configura redução de DC dos Especialistas para a próxima missão
        self.round_bonuses["DC_Reduction"] = especialistas
        if especialistas > 0:
            print(f"🧠 {especialistas} Especialista(s) ativo(s): Dificuldade da próxima missão reduzida em -{especialistas}.")
            
        input("\n[Pressione Enter para ir para a Loja]")


    def start(self):
        # CORREÇÃO: O jogo agora roda num loop infinito para permitir reinicializações
        while True:
            self.reset_game_state()
            self.choose_mode()
            
            game_over = False
            while self.current_level <= 5:
                if self.current_level > 1 and not [a for a in self.team if a.current_life > 0]:
                    print("\n💀 Todos os heróis do seu time morreram. Fim de jogo!")
                    game_over = True
                    break
                    
                self.shop_phase()
                success = self.run_mission()
                if not success:
                    game_over = True
                    break
            
            if not game_over and self.current_level > 5:
                print("\n" + "🎉"*15)
                print(" PARABÉNS! VOCÊ SUPEROU OS 5 NÍVEIS E ZEROU O JOGO! ")
                print("🎉"*15 + "\n")
            
            print("\n" + "="*50)
            print("                FIM DA PARTIDA                ")
            print("="*50)
            input("[Pressione Enter para reiniciar o jogo do zero]")
            print("\n\n" + "\n"*10) # Afasta o texto anterior da tela

# Inicialização
if __name__ == "__main__":
    # Nome exato da planilha atualizada que você enviou por último
    game = Game("plano.csv")
    game.start()