# 🎲 RPG Autobattler Roguelike

A turn-based strategy and management game built in Python. This project blends elements of *Autobattlers* (such as economy management and team composition building) with the high-stakes progression and survival challenges of *Roguelike* games.

---

## 🚀 Key Features

### 1. Shop & Economy System 🛒
* **Dynamic Reroll:** Refresh the market to find new heroes. The reroll cost increases progressively every time you use it within the same level to punish reckless spending.
* **Calibrated Probability:** Hero pooling is strictly based on rarity tiers (from 1 to 5 stars) using calibrated weights, ensuring a mathematically fair distribution.
* **Roster Management:** Build a team of up to 5 heroes. You can sell older characters to recover gold based on their rarity tier.

### 2. Agent Classes & Special Synergies ⚔️
Every hero plays a crucial role on the board, directly impacting the rest and preparation phase:
* **⚔️ Fighters (Lutadores):** Grant fury bonuses (Attack/Defense) in subsequent floors.
* **🛡️ Defenders (Defensores):** Generate stacking protective shields to absorb incoming damage.
* **🎯 Specialists (Especialistas):** Analyze environmental weaknesses, permanently reducing the target Difficulty Class (DC) of the mission.
* **❤️ Supports (Suportes):** Heal the remaining health points of surviving team members.

* **Pair & Group Synergies:** Characters with interconnected backstories (e.g., *Kazumi & Akane*, *Thoryn & Zendaya*) or from the same faction (*Heroes of Uagamora*, *Villains*, *The Flame*) gain massive stat boosts when deployed together.

### 3. Mission Phase & Skill Checks 🚨
* On each floor, your team faces challenges focused on a specific attribute (**Attack, Defense, or Skill**).
* The game automatically selects your team's best viable hero to perform the check.
* **D20 Dice Rolls:** Your hero performs **5 consecutive dice rolls** against the floor's Difficulty Class (DC), which increases with each level (up to level 5).
* **Fatigue:** As a hero performs checks, they accumulate fatigue in the active attribute, temporarily reducing their effectiveness.

### 4. The Roguelike Factor 💀
* **Classic Mode (Hard):** Activates the **Permadeath** mechanic. If a hero's health drops to 0, they are permanently purged from your team roster.
* **Extreme Criticals:** Rolling a natural `20` counts as 2 immediate successes. Rolling a natural `1` triggers a critical failure, dealing double damage to the hero.
* **Win Condition:** Survive and overcome all 5 floors of missions.
* **Loss Condition:** Accumulating 3 failures in a single mission results in an immediate *Game Over*.

---

## 🛠️ Technologies Used

* **Python 3.x** - Core programming language.
* **Tkinter** - Native Graphical User Interface (GUI) library.
* **Pandas** - Data manipulation and loading of the agent database via CSV.
* **Pillow (PIL)** - Processing, dynamic resizing, and rendering of hero images in JPG/PNG formats.

---

## 📂 File Structure

* `Arquivo.py`: Contains the GUI initialization, game state machine (Shop, Battle, Rest), rule calculations, and layout rendering.
* `plano.csv`: The hero database containing base stats (Attack, Defense, Health, Skill), lore descriptions, rarity tiers, and synergy tags[cite: 2].
* `Images/[agent_name]`: Character image files that visually populate the shop and battle cards (loaded dynamically in lowercase)[cite: 2].

---

## 🎮 How to Run the Game

1. Make sure you have Python installed along with the required libraries:
   ```bash
   pip install pandas Pillow

2. Run the following command in CMD
3. ```bash
   python Arquivo.py
