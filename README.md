# \# 🎴 RPG Autobattler Roguelike

# 

# Um jogo de estratégia \*Autobattler Roguelike\* baseado em turnos e texto/cartas, rodando diretamente no navegador (HTML5/CSS3/JavaScript). O projeto revive e homenageia \*\*47 personagens icônicos\*\* de campanhas clássicas de RPG de um grupo de amigos, misturando gerenciamento de economia, sinergias de equipe e um sistema tático de fadiga contra missões de alta dificuldade.

# 

# \---

# 

# \## 🎮 Conceito \& Gameplay Loop

# 

# O jogo se divide em duas fases principais que se repetem ao longo de \*\*5 níveis progressivos\*\*:

# 

# 1\. \*\*Fase de Loja (Descanso):\*\* O jogador gerencia suas moedas para comprar heróis únicos, atualizar o mercado (\*reroll\*) ou vender membros para reestruturar a equipe. O time deve ter entre \*\*3 e 5 personagens\*\*.

# 2\. \*\*Fase de Missão (Combate):\*\* O time enfrenta um bloco de \*\*5 testes sequenciais d20\*\*. A cada teste, o herói mais apto do tabuleiro realiza a ação, sofrendo \*\*fadiga\*\* logo em seguida.

# 

# \---

# 

# \## ⚙️ Mecânicas Centrais

# 

# \### 💰 Economia \& Regras do Mercado

# \* \*\*Orçamento Inicial:\*\* 10 Moedas.

# \* \*\*Reroll da Loja:\*\* 1 Moeda por atualização (3 slots de heróis por vez).

# \* \*\*Venda:\*\* Retorno de 100% do custo original do personagem.

# \* \*\*Chances de Raridade no Mercado (Por Slot):\*\*

# &#x20; \* Custo 1 (Comum): \*\*40%\*\*

# &#x20; \* Custo 2 (Incomum): \*\*30%\*\*

# &#x20; \* Custo 3 (Raro): \*\*15%\*\*

# &#x20; \* Custo 4 (Épico): \*\*10%\*\*

# &#x20; \* Custo 5 (Lendário): \*\*5%\*\*

# \* \*\*Regra de Exclusão Única:\*\* Não existem cópias. Se um herói está no seu time, ele é removido do banco do mercado. O mercado nunca gera duplicatas no mesmo \*roll\*.

# 

# \### 🧬 Sistema de Sinergias (Tags de Histórico)

# Colocar personagens com laços históricos juntos no tabuleiro ativo concede bônus numéricos brutos para os testes:

# \* \*\*Duplas Específicas:\*\* (\*Ex: Kazumi \& Akane, Thoryn \& Zendaya\*) concedem \*\*+3 em todos os testes\*\* para ambos.

# \* \*\*Grupos/Facções:\*\* (\*Ex: Heróis de Uagamora, Hara Kiri, A Chama\*) escalam conforme o número de membros ativos no time:

# &#x20; \* 3 membros do mesmo grupo: \*\*+2\*\* para todos eles.

# &#x20; \* 4 membros do mesmo grupo: \*\*+3\*\* para todos eles.

# &#x20; \* 5 membros do mesmo grupo: \*\*+4\*\* para todos eles.

# 

# \### ⏳ Testes, Fadiga e Críticos

# Em cada um dos 5 testes de uma missão, o motor do jogo seleciona o herói vivo com o \*\*maior atributo atual\*\* exigido pela fase (Ataque, Defesa ou Perícia).

# \* \*\*A Rolagem:\*\* Atributo Atual + 1d20 + Bônus Temporários vs. DC do Nível.

# \* \*\*Mecânica de Fadiga:\*\* Independente de vencer ou falhar, o herói que realizou o teste recebe um \*\*debuff temporário de $-x$\*\* (onde $x$ é o nível da missão atual) naquele atributo. Isso força o jogador a rotacionar o time.

# \* \*\*O Fator Destino (Dados Críticos):\*\*

# &#x20; \* \*\*20 Natural:\*\* Sucesso Crítico! Garante \*\*2 Sucessos\*\* imediatos na contagem da missão.

# &#x20; \* \*\*1 Natural:\*\* Falha Crítica! Conta como \*\*2 Fracassos\*\* e o herói sofre o dobro de dano.

# 

# \---

# 

# \## 🎭 Modos de Jogo \& Dificuldade

# 

# O progresso é ditado pela dificuldade dos testes por andar:

# \* \*\*Nível 1:\*\* DC 10 (Falha: 1 de dano)

# \* \*\*Nível 2:\*\* DC 13 (Falha: 2 de dano)

# \* \*\*Nível 3:\*\* DC 16 (Falha: 3 de dano)

# \* \*\*Nível 4:\*\* DC 20 (Falha: 4 de dano)

# \* \*\*Nível 5:\*\* DC 25 (Falha: 5 de dano)

# 

# Ao final do nível, avalia-se o placar: \*\*0-2 Sucessos\*\* (Fracasso/Game Over), \*\*3 Sucessos\*\* (Vitória Pírrica / +2g), \*\*4 Sucessos\*\* (Vitória Confiante / +5g) ou \*\*5 Sucessos\*\* (Vitória Absoluta / +8g).

# 

# \### 🟢 Modo Padrão (Fácil)

# Personagens cuja vida zera em missão ficam apenas \*\*Nocauteados\*\*. Eles não podem mais fazer testes naquele nível, mas despertam na Fase de Descanso com 1 de Vida.

# 

# \### 🔴 Modo Clássico (Difícil)

# \*\*Morte Permanente (Permadeath).\*\* Se a vida de um herói chegar a 0, ele é deletado do jogo para sempre. O jogador deve prosseguir a campanha com o que sobrou no tabuleiro, mesmo que isso signifique lutar com apenas 1 ou 2 personagens.

# 

# \---

# 

# \## 🏛️ Passivas de Classe (Fim de Rodada)

# Ao passar de nível, a fadiga limpa, o jogador recebe um salário fixo de \*\*+5 Moedas\*\* e as classes do time ativam bônus cumulativos para o próximo nível (com \*\*teto máximo de +3\*\* por classe):

# 

# \* \*\*Suportes (Cura):\*\* Cada Suporte cura \*\*+1 de Vida\*\* de todos os aliados.

# \* \*\*Defensores (Escudo):\*\* Cada Defensor concede \*\*+1 de Escudo temporário\*\* (vida extra absorvedora de dano) para o time.

# \* \*\*Lutadores (Fúria):\*\* Cada Lutador concede \*\*+1 de bônus direto no resultado final\*\* do próximo dado do time.

# \* \*\*Especialistas (Planejamento):\*\* Cada Especialista reduz a dificuldade base (DC) de todos os testes do próximo nível em \*\*-1\*\*.

# 

# \---

# 

# \## 🎨 Design de Interface Sugerido (UI/UX)

# 

# As cartas devem ser estilizadas via CSS Grid/Flexbox respeitando a paleta de cores de sua raridade:

# \* \*\*Custo 1 (Comum):\*\* Borda cinza rústica.

# \* \*\*Custo 2 (Incomum):\*\* Borda bronze/verde opaco.

# \* \*\*Custo 3 (Raro):\*\* Borda azul rúnica brilhante.

# \* \*\*C

