import pandas as pd
import random
import os
import tkinter as tk
import math
from tkinter import messagebox, ttk
# Importações necessárias para manipular imagens JPG/PNG no Tkinter
from PIL import Image, ImageTk 


class Agent:
    def __init__(self, row):
        self.name = row['Agente']
        self.type = row['Tipo']
        self.desc = row['Descrição']
        self.player = row['Player']
        self.rarity = int(row['Raridade'])
        self.synergy_text = str(row['Sinergia']).strip()
        
        self.base_attack = int(row['Ataque'])
        self.base_defense = int(row['Defesa'])
        self.max_life = int(row['Vida'])
        self.base_skill = int(row['Perícia'])
        
        self.current_life = self.max_life
        self.fatigue = {"Ataque": 0, "Defesa": 0, "Perícia": 0}

        # --- CONFIGURAÇÃO DA IMAGEM ---
        # Converte o nome para minúsculo para evitar problemas (Ex: "Orion" vira "orion.jpg")
        # Você pode alterar essa lógica se preferir colocar as imagens em uma pasta específica,
        # por exemplo: f"imagens/{self.name.lower()}.jpg"
        self.image_filename = f"{self.name.lower()}"

    def reset_status(self):
        self.current_life = self.max_life
        self.fatigue = {"Ataque": 0, "Defesa": 0, "Perícia": 0}

    def reset_fatigue(self):
        self.fatigue = {"Ataque": 0, "Defesa": 0, "Perícia": 0}

    def get_attr(self, attr_name, team_agents):
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
        
        pairs = [
            ("Kazumi", "Akane"), ("Thoryn", "Zendaya"), 
            ("Perdigas", "Maria Cecília"), ("Tom", "Maria Cecília"), 
            ("Saphyra", "Shantal")
        ]
        for p1, p2 in pairs:
            if self.name in (p1, p2):
                if p1 in names_in_team and p2 in names_in_team:
                    bonus += 3

        groups = ["Heróis de Uagamora", "Heróis do Ninho", "Hara Kiri", "A Chama", "A Banda", "Vilões", "Vendedor", "Dragão"]
        for g in groups:
            if g.lower() in self.synergy_text.lower() or (g == "Vilões" and "vilão" in self.synergy_text.lower()):
                count = 0
                for a in team_agents:
                    if g.lower() in a.synergy_text.lower() or (g == "Vilões" and "vilão" in a.synergy_text.lower()):
                        count += 1
                if count == 3: bonus += 2
                elif count == 4: bonus += 3
                elif count >= 5: bonus += 4
        return bonus


class GameGUI:
    def __init__(self, root, csv_path):
        self.root = root
        self.root.title("RPG Autobattler Roguelike")
        self.root.geometry("950x700") # Aumentado ligeiramente para comportar as imagens confortavelmente
        self.root.configure(bg="#1e1e1e")
        
        # Cores por Raridade
        self.rarity_colors = {
            1: "#b0b0b0", # Comum - Cinza
            2: "#4caf50", # Incomum - Verde
            3: "#2196f3", # Raro - Azul
            4: "#9c27b0", # Épico - Roxo
            5: "#ff9800"  # Lendário - Laranja/Dourado
        }

        # Carregar Banco de Dados
        self.base_dir = os.path.dirname(os.path.abspath(__file__))
        full_path = os.path.join(self.base_dir, csv_path)
        try:
            self.df = pd.read_csv(full_path)
        except FileNotFoundError:
            messagebox.showerror("Erro", f"Arquivo {csv_path} não encontrado no diretório atual.")
            self.root.quit()
            return
            
        self.all_agents = [Agent(row) for _, row in self.df.iterrows()]
        
        # Dicionário para manter o cache de imagens carregadas na memória do Tkinter
        # Isso impede que o Garbage Collector do Python delete as imagens da tela
        self.image_cache = {}

        # Inicializar Estado
        self.reset_game_state()
        self.create_mode_selection_screen()

    def mapping_types(self, agentType):
        typeName = ""
        if agentType == "Lutador":
            typeName = "⚔️"
        elif agentType == "Defensor":
            typeName = "🛡️"
        elif agentType == "Especialista":
            typeName = "🎯"
        elif agentType == "Suporte":
            typeName = "❤️"

        return typeName

    def reset_game_state(self):
        for a in self.all_agents:
            a.reset_status()
        self.team = []
        self.gold = 10
        self.current_level = 1
        self.mode = "Difícil"
        self.round_bonuses = {"Ataque": 0, "Defesa": 0, "Escudo": 0, "DC_Reduction": 0}
        self.market = []

    def clear_screen(self):
        for widget in self.root.winfo_children():
            widget.destroy()

    def create_mode_selection_screen(self):
        self.clear_screen()
        
        frame = tk.Frame(self.root, bg="#1e1e1e")
        frame.place(relx=0.5, rely=0.5, anchor="center")
        
        title = tk.Label(frame, text="RPG AUTOBATTLER ROGUELIKE", font=("Arial", 24, "bold"), fg="#ffffff", bg="#1e1e1e")
        title.pack(pady=20)
        
        btn_normal = tk.Button(frame, text="Modo Clássico (Normal)", font=("Arial", 14), bg="#0a6d27", fg="white", width=30, height=3, command=lambda: self.start_game("Normal"))
        btn_normal.pack(pady=10)

        btn_hard = tk.Button(frame, text="Modo Difícil\n[Permadeath e Críticos 2x]", font=("Arial", 14), bg="#f44336", fg="white", width=30, height=3, command=lambda: self.start_game("Difícil"))
        btn_hard.pack(pady=10)

    def start_game(self, mode):
        self.mode = mode
        self.market = self.roll_market()
        self.create_shop_screen()

    def get_market_pool(self):
        team_names = [a.name for a in self.team]
        return [a for a in self.all_agents if a.name not in team_names]

    def roll_market(self):
        pool = self.get_market_pool()
        if not pool: return []
        
        # Mapeia diretamente a raridade para a probabilidade estipulada
        rarity_weights = {1: 0.40, 2: 0.30, 3: 0.15, 4: 0.10, 5: 0.05}
        
        market_slots = []
        
        # Queremos preencher até 4 slots (ou o máximo de heróis que restarem na pool)
        slots_necessarios = min(4, len(pool))
        
        while len(market_slots) < slots_necessarios:
            # 1. Calcula o peso de cada herói que sobrou na pool e que ainda não foi listado nesta rolagem
            opcoes_disponiveis = [a for a in pool if a not in market_slots]
            if not opcoes_disponiveis:
                break
                
            # Define o peso individual de cada herói com base na raridade dele
            pesos_dos_herois = [rarity_weights.get(a.rarity, 0.0) for a in opcoes_disponiveis]
            
            # Se por acaso a soma dos pesos for zero (ex: só sobraram heróis com peso zerado), 
            # define peso igual para o que sobrar para o jogo não quebrar
            if sum(pesos_dos_herois) == 0:
                pesos_dos_herois = [1] * len(opcoes_disponiveis)
            
            # 2. Sorteia exatamente 1 herói usando os pesos calibrados
            heroi_escolhido = random.choices(opcoes_disponiveis, weights=pesos_dos_herois, k=1)[0]
            market_slots.append(heroi_escolhido)
            
        return market_slots

    # --- FUNÇÃO AUXILIAR PARA CARREGAR IMAGEM ---
    def get_agent_image(self, agent, size=(50, 50)):
        """Carrega e redimensiona a imagem do agente. Retorna um placeholder se não encontrar."""
        if agent.name in self.image_cache:
            return self.image_cache[agent.name]

        img_path = os.path.join(self.base_dir, "Imagens", agent.image_filename + ".png")
        if(not os.path.exists(img_path)):
            img_path = os.path.join(self.base_dir, "Imagens", agent.image_filename + ".jpg")
        
        if os.path.exists(img_path):
            try:
                img = Image.open(img_path)
                img = img.resize(size, Image.Resampling.LANCZOS)
                photo = ImageTk.PhotoImage(img)
                self.image_cache[agent.name] = photo
                return photo
            except Exception:
                pass # Caso dê falha ao ler o arquivo, cai no bloco abaixo
        
        # Cria um quadrado cinza vazio (Placeholder) caso o arquivo de imagem não exista
        placeholder = Image.new("RGB", size, color="#555555")
        photo = ImageTk.PhotoImage(placeholder)
        self.image_cache[agent.name] = photo
        return photo

    def create_shop_screen(self):
        self.clear_screen()
        
        # Painel Superior (Info)
        top_frame = tk.Frame(self.root, bg="#2d2d2d", height=60)
        top_frame.pack(fill="x", side="top")
        
        info_lbl = tk.Label(top_frame, text=f"Missão Atual: Nível {self.current_level}  |  Modo: {self.mode}  |  💰 Ouro: {self.gold}g", font=("Arial", 14, "bold"), fg="#ffffff", bg="#2d2d2d")
        info_lbl.pack(side="left", padx=20, pady=15)
        
        btn_mission = tk.Button(top_frame, text="⚔️ IR PARA MISSÃO", font=("Arial", 12, "bold"), bg="#ff5722", fg="white", command=self.check_go_to_mission)
        btn_mission.pack(side="right", padx=20, pady=10)
        
        btn_reroll = tk.Button(top_frame, text=f"🔄 Reroll Loja ({self.count}g)", font=("Arial", 12), bg="#795548", fg="white", command=self.reroll_shop)
        btn_reroll.pack(side="right", padx=10, pady=10)

        # Container Principal
        main_frame = tk.Frame(self.root, bg="#1e1e1e")
        main_frame.pack(fill="both", expand=True, pady=10)

       # Seção do Mercado (Esquerda)
        market_frame = tk.LabelFrame(main_frame, text=" Mercado (4 slots) ", font=("Arial", 12, "bold"), fg="white", bg="#1e1e1e", labelanchor="n")
        market_frame.pack(side="left", fill="both", expand=True, padx=10, pady=5)
        
        # Configura o market_frame para distribuir as duas colunas igualmente
        market_frame.grid_columnconfigure(0, weight=1)
        market_frame.grid_columnconfigure(1, weight=1)
        market_frame.grid_rowconfigure(0, weight=1)
        market_frame.grid_rowconfigure(1, weight=1)

        for i, agent in enumerate(self.market):
            # Calcula a linha (row) e coluna (col) para fazer uma grade 2x2
            row = i // 2
            col = i % 2

            card = tk.Frame(market_frame, bg="#2a2a2a", bd=2, relief="groove")
            # Usamos .grid() em vez de .pack() para posicionar o quadrado na matriz 2x2
            card.grid(row=row, column=col, padx=10, pady=10, sticky="nsew")
            
            if agent is None:
                lbl = tk.Label(card, text=" COMPRADO ", font=("Arial", 12, "italic"), fg="#777777", bg="#2a2a2a")
                lbl.pack(expand=True, pady=10)
            else:
                color = self.rarity_colors.get(agent.rarity, "white")
                
                # 1. Nome do personagem no topo do quadrado
                title_lbl = tk.Label(card, text=f"[{i+1}] {agent.name}\n({self.mapping_types(agent.type)})", font=("Arial", 11, "bold"), fg=color, bg="#2a2a2a")
                title_lbl.pack(pady=5)
                
                # 2. IMAGEM MAIOR (Aumentei o tamanho de 50x50 para 100x100)
                # Você pode ajustar o (100, 100) abaixo para o tamanho que preferir!
                img_label = tk.Label(card, image=self.get_agent_image(agent, (100, 100)), bg="#2a2a2a")
                img_label.pack(pady=5)
                
                # 3. Atributos centralizados abaixo da imagem
                if self.mode == "Difícil":
                    stats_str = f"🪙 {agent.rarity}g\n⚔️ ATK: ?  🛡️ DEF: ?\n🎯 PER: ?  ❤️ HP: {agent.max_life}"
                else: 
                    stats_str = f"🪙 {agent.rarity}g\n⚔️ ATK: {agent.base_attack}  🛡️ DEF: {agent.base_defense}\n🎯 PER: {agent.base_skill}  ❤️ HP: {agent.max_life}"
                stats_lbl = tk.Label(card, text=stats_str, font=("Arial", 10), fg="#bbbbbb", bg="#2a2a2a", justify="center")
                stats_lbl.pack(pady=5)
                
                # 4. Botão de compra no rodapé do quadrado
                btn_buy = tk.Button(card, text="Comprar", bg="#4caf50", fg="white", font=("Arial", 10, "bold"), command=lambda idx=i: self.buy_agent(idx))
                btn_buy.pack(side="bottom", fill="x", padx=10, pady=5)
                btn_buy.pack(side="bottom", fill="x", padx=10, pady=5)
        # Seção do Time (Direita)
        team_frame = tk.LabelFrame(main_frame, text=" Sua Equipe (Mín 1 / Máx 5) ", font=("Arial", 12, "bold"), fg="white", bg="#1e1e1e", labelanchor="n")
        team_frame.pack(side="right", fill="both", expand=True, padx=10, pady=5)
        
        if not self.team:
            tk.Label(team_frame, text="(Nenhum herói no time)", fg="#888888", bg="#1e1e1e", font=("Arial", 12)).pack(pady=50)
            
        for i, agent in enumerate(self.team):
            card = tk.Frame(team_frame, bg="#333333", bd=1, relief="flat")
            card.pack(fill="x", padx=10, pady=5)
            
            # --- CONTAINER DA IMAGEM NO TIME ---
            img_label = tk.Label(card, image=self.get_agent_image(agent, (45, 45)), bg="#333333")
            img_label.pack(side="left", padx=5, pady=5)
            
            info_container = tk.Frame(card, bg="#333333")
            info_container.pack(side="left", fill="both", expand=True)

            color = self.rarity_colors.get(agent.rarity, "white")
            title_lbl = tk.Label(info_container, text=f"{agent.name} ({self.mapping_types(agent.type)})", font=("Arial", 11, "bold"), fg=color, bg="#333333")
            title_lbl.pack(anchor="w", padx=5, pady=2)
            
            hp_str = f"❤️ Vida: {agent.current_life}/{agent.max_life}"
            stats_lbl = tk.Label(info_container, text=f"{hp_str} | ⚔️ ATK:{agent.base_attack} 🛡️ DEF:{agent.base_defense} 🎯 PER:{agent.base_skill}", font=("Arial", 10), fg="#e0e0e0", bg="#333333")
            stats_lbl.pack(anchor="w", padx=5)
            
            btn_sell = tk.Button(card, text=f"Vender (+{math.ceil(agent.rarity/2)}g)", bg="#f44336", fg="white", font=("Arial", 9), command=lambda idx=i: self.sell_agent(idx))
            btn_sell.pack(side="right", padx=5, pady=2)
    
    count = 1
    def reroll_shop(self):
        if self.gold == self.count and len(self.team) < 1:
            messagebox.showwarning("Aviso", "É necessário ter moedas suficiente para um personagem!")
            return

        if self.gold >= self.count:
            self.gold -= self.count
            self.count += 1
            self.market = self.roll_market()
            self.create_shop_screen()
        else:
            messagebox.showwarning("Aviso", "Moedas insuficientes para Reroll!")

    def buy_agent(self, idx):
        agent = self.market[idx]
        if agent is None: return
        
        if self.gold >= agent.rarity:
            if len(self.team) < 5:
                self.gold -= agent.rarity
                self.team.append(agent)
                self.market[idx] = None
                self.create_shop_screen()
            else:
                messagebox.showwarning("Aviso", "Equipe cheia (máximo 5 personagens)!")
        else:
            messagebox.showwarning("Aviso", "Moedas insuficientes!")

    def sell_agent(self, idx):
        removed = self.team.pop(idx)
        self.gold += math.ceil(removed.rarity/2)
        self.create_shop_screen()

    def check_go_to_mission(self):
        pode_dar_reroll = (self.gold >= self.count)
        pode_comprar_algo = any(a is not None and self.gold >= a.rarity for a in self.market)
        esta_travado = (not pode_dar_reroll) and (not pode_comprar_algo)

        if len(self.team) > 5:
            messagebox.showwarning("Aviso", "Sua equipe excede o limite máximo de 5 personagens!")
        elif len(self.team) == 0:
            messagebox.showwarning("Aviso", "Você não pode ir para a missão com o time vazio!")
        else:
            self.start_mission_phase()
        

    def start_mission_phase(self):
        self.clear_screen()
        
        themes = ["Ataque", "Defesa", "Perícia"]
        self.current_theme = random.choice(themes)
        
        if self.mode == "Difícil":
            dc_table = {1: 13, 2: 16, 3: 19, 4: 23, 5: 27}
        
        else: 
            dc_table = {1: 10, 2: 13, 3: 16, 4: 20, 5: 25}

        base_dc = dc_table[self.current_level]
        self.dc = max(1, base_dc - self.round_bonuses["DC_Reduction"])
        
        self.shields = {a.name: self.round_bonuses["Escudo"] for a in self.team}
        self.sucessos = 0
        self.falhas = 0
        self.teste_num = 1
        
        self.battle_frame = tk.Frame(self.root, bg="#121212")
        self.battle_frame.pack(fill="both", expand=True, padx=20, pady=20)
        
        self.lbl_m_title = tk.Label(self.battle_frame, text=f"🚨 MISSÃO NÍVEL {self.current_level} | TEMA: {self.current_theme.upper()} 🚨", font=("Arial", 16, "bold"), fg="#ff9800", bg="#121212")
        self.lbl_m_title.pack(pady=10)
        
        self.lbl_dc_info = tk.Label(self.battle_frame, text=f"Dificuldade Alvo (DC): {self.dc}  (DC Base: {base_dc})", font=("Arial", 12), fg="white", bg="#121212")
        self.lbl_dc_info.pack()

        # Adicionado um painel para exibir o herói ativo que vai rolar os dados na missão
        self.hero_battle_panel = tk.Frame(self.battle_frame, bg="#121212")
        self.hero_battle_panel.pack(pady=5)
        self.lbl_battle_img = tk.Label(self.hero_battle_panel, bg="#121212")
        self.lbl_battle_img.pack()

        self.log_txt = tk.Text(self.battle_frame, bg="#1e1e1e", fg="#00ff00", font=("Consolas", 11), height=12, state="disabled")
        self.log_txt.pack(fill="x", pady=10, padx=10)
        
        self.btn_roll = tk.Button(self.battle_frame, text="🎲 ROLAR DADO (TESTE 1/5)", font=("Arial", 14, "bold"), bg="#e91e63", fg="white", height=2, command=self.next_test_roll)
        self.btn_roll.pack(pady=10)
        
        self.append_log("Fase preparada. Identificando combatentes viáveis...")
        self.setup_next_combatant_info()

    def append_log(self, text, cor="white"):
        self.log_txt.config(state="normal")

        # 1. Configura a tag dinamicamente.
        # Isso diz ao Tkinter: "A tag com o nome 'red' deve ter o texto vermelho (foreground)"
        self.log_txt.tag_config(cor, foreground=cor)

        # 2. Insere o texto aplicando a tag que configuramos acima
        self.log_txt.insert(tk.END, text + "\n", cor)

        self.log_txt.see(tk.END)
        self.log_txt.config(state="disabled")

    def setup_next_combatant_info(self):
        alive_heroes = [a for a in self.team if a.current_life > 0]
        if not alive_heroes or self.falhas >= 3:
            self.end_mission_calculations(False)
            return
            
        self.best_hero = None
        self.best_val = -999
        
        for a in alive_heroes:
            val = a.get_attr(self.current_theme, self.team)
            if self.current_theme == "Ataque": val += self.round_bonuses["Ataque"]
            elif self.current_theme == "Defesa": val += self.round_bonuses["Defesa"]
            
            if val > self.best_val:
                self.best_val = val
                self.best_hero = a
                
        self.btn_roll.config(text=f"🎲 ROLAR PARA {self.best_hero.name.upper()} (Total: {self.best_val}) [Teste {self.teste_num}/5]")
        
        # --- ATUALIZA A IMAGEM DO HERÓI NA TELA DE BATALHA ---
        battle_photo = self.get_agent_image(self.best_hero, (80, 80))
        self.lbl_battle_img.config(image=battle_photo)

    def next_test_roll(self):

        VERDE = "\033[92m"
        AMARELO = "\033[93m"
        VERMELHO = "\033[91m"
        RESET = "\033[0m"

        d20 = random.randint(1, 20)
        total = self.best_val + d20
        
        self.append_log(f"\n--- TESTE {self.teste_num}/5 ({self.best_hero.name}) ---")
        self.append_log(f"Resultado do dado: {d20} | Total: {total} vs Alvo {self.dc}")
        
        if self.mode == "Difícil" and d20 == 20:
            self.append_log(f"🌟 CRÍTICO POSITIVO! Contando como 2 SUCESSOS!", cor="green")
            self.sucessos += 2
        elif self.mode == "Difícil" and d20 == 1:
            self.append_log(f"💀 CRÍTICO NEGATIVO! 1 Falha Crítica anotada.", cor="red")
            self.falhas += 2
            dano = self.current_level * 2
            self.apply_gui_damage(self.best_hero, dano)
        elif total >= self.dc:
            self.append_log(f"🟢 SUCESSO!", cor="green")
            self.sucessos += 1
        else:
            self.append_log(f"🔴 FALHA!", cor="red")
            self.falhas += 1
            dano = self.current_level
            self.apply_gui_damage(self.best_hero, dano)
        self.best_hero.fatigue[self.current_theme] += 1

        self.teste_num += 1
        
        if self.falhas >= 3 or self.teste_num > 5:
            self.btn_roll.config(text="VER RESULTADO FINAL", bg="#2196f3", command=self.end_mission_calculations)
        else:
            self.setup_next_combatant_info()

    def apply_gui_damage(self, hero, dano):
        if self.shields.get(hero.name, 0) > 0:
            if self.shields[hero.name] >= dano:
                self.shields[hero.name] -= dano
                self.append_log(f"🛡️ Escudo absorveu o golpe completamente. Restante: {self.shields[hero.name]}")
                return
            else:
                dano -= self.shields[hero.name]
                self.append_log(f"🛡️ Escudo quebrou! Absorveu parte. {dano} de dano vaza para o HP.")
                self.shields[hero.name] = 0
                
        hero.current_life -= dano
        self.append_log(f"💥 Dano sofrido por {hero.name}: {dano} HP. Atual: {max(0, hero.current_life)}/{hero.max_life}")
        
        if hero.current_life <= 0:
            if self.mode == "Difícil":
                self.append_log(f"☠️ PERMADEATH: {hero.name} morreu e foi expurgado da equipe.")
                self.team.remove(hero)
            else:
                hero.current_life = 0
                self.append_log(f"💤 NOCAUTE: {hero.name} desmaiou.")

    def end_mission_calculations(self, aliveHeroes = True):
        self.clear_screen()
        
        for a in self.team:
            a.reset_fatigue()
            
        frame = tk.Frame(self.root, bg="#1e1e1e")
        frame.place(relx=0.5, rely=0.5, anchor="center")
        
        lbl_title = tk.Label(frame, text=f"RESULTADO FINAL: {self.sucessos} SUCESSOS | {self.falhas} FALHAS", font=("Arial", 16, "bold"), fg="white", bg="#1e1e1e")
        lbl_title.pack(pady=20)
        
        if not aliveHeroes:
            lbl_res = tk.Label(frame, text="🔴 GAME OVER!\nSua equipe inteira está morta!", font=("Arial", 14, "bold"), fg="#f44336", bg="#1e1e1e")
            lbl_res.pack(pady=10)
            btn_continue = tk.Button(frame, text="REINICIAR JOGO", font=("Arial", 12, "bold"), bg="#f44336", fg="white", command=self.restart_entire_game)
            btn_continue.pack(pady=20)

        elif self.falhas >= 3:
            lbl_res = tk.Label(frame, text="🔴 GAME OVER!\nSua equipe acumulou 3 ou mais falhas e não conseguiu completar o andar.", font=("Arial", 14, "bold"), fg="#f44336", bg="#1e1e1e")
            lbl_res.pack(pady=10)
            btn_continue = tk.Button(frame, text="REINICIAR JOGO", font=("Arial", 12, "bold"), bg="#f44336", fg="white", command=self.restart_entire_game)
            btn_continue.pack(pady=20)

        else:
            if self.sucessos == 3:
                txt, g = "🟡 VITÓRIA PÍRRICA! Vocês avançaram no limite.", 2
            elif self.sucessos == 4:
                txt, g = "🔵 VITÓRIA CONFIANTE! Uma excelente exibição tática.", 5
            else:
                txt, g = "🟢 VITÓRIA ABSOLUTA! Perfeito e lendário!", 8
                
            self.gold += g
            lbl_res = tk.Label(frame, text=f"{txt}\nRecompensa da Fase: +{g}g", font=("Arial", 14, "bold"), fg="#4caf50", bg="#1e1e1e")
            lbl_res.pack(pady=10)
            
            btn_continue = tk.Button(frame, text="AVANÇAR PARA DESCANSO", font=("Arial", 12, "bold"), bg="#4caf50", fg="white", command=self.next_level_rest_phase)
            btn_continue.pack(pady=20)

    def next_level_rest_phase(self):
        self.clear_screen()
        self.gold += 5  # Salário Base
        
        suportes = min(3, sum(1 for a in self.team if a.type == "Suporte" and a.current_life > 0))
        defensores = min(3, sum(1 for a in self.team if a.type == "Defensor" and a.current_life > 0))
        lutadores = min(3, sum(1 for a in self.team if a.type == "Lutador" and a.current_life > 0))
        especialistas = min(3, sum(1 for a in self.team if a.type == "Especialista" and a.current_life > 0))
        
        frame = tk.Frame(self.root, bg="#1e1e1e")
        frame.place(relx=0.5, rely=0.5, anchor="center")
        
        tk.Label(frame, text="☕ FASE DE DESCANSO", font=("Arial", 18, "bold"), fg="white", bg="#1e1e1e").pack(pady=10)
        
        log_box = tk.Text(frame, bg="#2d2d2d", fg="#ffffff", font=("Arial", 11), width=50, height=8, bd=0)
        log_box.pack(pady=10)
        
        def add_msg(msg): log_box.insert(tk.END, msg + "\n")
        
        add_msg("💰 +5g de salário base depositados.")

        if suportes > 0:
            for a in self.team:
                if a.current_life > 0: a.current_life = min(a.max_life, a.current_life + suportes)
            add_msg(f"💚 {suportes} Suporte(s): Time recuperou +{suportes} de Vida.")
            
        self.round_bonuses["Escudo"] = defensores
        if defensores > 0:
            add_msg(f"🛡️ {defensores} Defensor(es): +{defensores} de Escudo para o próximo andar.")
            
        self.round_bonuses["Ataque"] = lutadores
        self.round_bonuses["Defesa"] = lutadores
        if lutadores > 0:
            add_msg(f"🔥 {lutadores} Lutador(es): +{lutadores} de Fúria nos testes seguintes.")
            
        self.round_bonuses["DC_Reduction"] = especialistas
        if especialistas > 0:
            add_msg(f"🧠 {especialistas} Especialista(s): DC alvo reduzida em -{especialistas}.")
            
        log_box.config(state="disabled")
        
        self.current_level += 1
        
        if self.current_level > 5:
            btn_text = "VER TELA DE VITÓRIA 🎉"
            cmd = self.show_victory_screen
        else:
            btn_text = "IR PARA A LOJA 🛒"
            cmd = lambda: [setattr(self, 'market', self.roll_market()), self.create_shop_screen()]
            self.count = 1
            
        tk.Button(frame, text=btn_text, font=("Arial", 12, "bold"), bg="#2196f3", fg="white", command=cmd).pack(pady=10)

    def show_victory_screen(self):
        self.clear_screen()
        frame = tk.Frame(self.root, bg="#1e1e1e")
        frame.place(relx=0.5, rely=0.5, anchor="center")
        
        tk.Label(frame, text="🎉 PARABÉNS! 🎉", font=("Arial", 28, "bold"), fg="#ff9800", bg="#1e1e1e").pack(pady=10)
        tk.Label(frame, text="VOCÊ SUPEROU OS 5 NÍVEIS E ZEROU O JOGO!", font=("Arial", 16, "bold"), fg="white", bg="#1e1e1e").pack(pady=10)
        
        tk.Button(frame, text="JOGAR NOVAMENTE", font=("Arial", 12, "bold"), bg="#4caf50", fg="white", command=self.restart_entire_game).pack(pady=20)

    def restart_entire_game(self):
        self.reset_game_state()
        self.create_mode_selection_screen()


if __name__ == "__main__":
    root = tk.Tk()
    app = GameGUI(root, "plano.csv")
    root.mainloop()