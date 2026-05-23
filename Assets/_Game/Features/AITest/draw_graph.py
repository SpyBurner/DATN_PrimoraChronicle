import re
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np

results_path = r"c:\Primora\DATN_PrimoraChronicle\Assets\_Game\Features\AITest\ai_battle_results.md"

with open(results_path, "r", encoding="utf-8") as f:
    content = f.read()

# Parse summary
total_games = int(re.search(r"\*\*Total Games:\*\* (\d+)", content).group(1))
player_count_match = re.search(r"\*\*Player Count:\*\* (\d+)", content)
player_count = int(player_count_match.group(1)) if player_count_match else 2

player_wins = []
for i in range(player_count):
    m = re.search(rf"\*\*Player {i}.*?Wins:\*\* (\d+)", content)
    player_wins.append(int(m.group(1)) if m else 0)

# Parse table rows dynamically
# Each row: | Game | (HP | ATK | SPD | Skills) x N | Winner | Moves | AvgMoveTime |
row_pattern = r"\| (\d+) \|"
for i in range(player_count):
    row_pattern += r" (\d+) \| (\d+) \| (\d+) \| (.+?) \|"
row_pattern += r" (\d+) \| (\d+) \| ([\d.]+) \|"

rows = re.findall(row_pattern, content)

games, winners, moves, avg_time = [], [], [], []
player_hp = [[] for _ in range(player_count)]
player_atk = [[] for _ in range(player_count)]
player_spd = [[] for _ in range(player_count)]

for row in rows:
    idx = 0
    games.append(int(row[idx])); idx += 1
    for p in range(player_count):
        player_hp[p].append(int(row[idx])); idx += 1
        player_atk[p].append(int(row[idx])); idx += 1
        player_spd[p].append(int(row[idx])); idx += 1
        idx += 1  # skip skills
    winners.append(int(row[idx])); idx += 1
    moves.append(int(row[idx])); idx += 1
    avg_time.append(float(row[idx]))

games = np.array(games)
winners = np.array(winners)
moves = np.array(moves)
avg_time = np.array(avg_time)

# Colors for each player
player_colors = ['#4CAF50', '#F44336', '#2196F3', '#FF9800', '#9C27B0', '#00BCD4']

def player_label(i):
    return "AI" if i == 0 else f"Player {i}"

def player_short(i):
    return "AI" if i == 0 else f"P{i}"

fig, axes = plt.subplots(2, 3, figsize=(18, 10))
title_parts = [f"{player_short(i)}: {player_wins[i]} ({player_wins[i]/total_games*100:.1f}%)" for i in range(player_count)]
fig.suptitle(f"AI Battle Results — {total_games} Games | {' | '.join(title_parts)}", fontsize=13, fontweight='bold')

# 1. Win rate pie chart
ax = axes[0, 0]
labels = [player_label(i) for i in range(player_count)]
colors = player_colors[:player_count]
ax.pie(player_wins, labels=labels, autopct='%1.1f%%', colors=colors, startangle=90)
ax.set_title("Win Rate")

# 2. Cumulative win rate over games
ax = axes[0, 1]
for i in range(player_count):
    cum = np.cumsum(winners == i)
    ax.plot(games, cum / games * 100, label=player_label(i), color=player_colors[i], linewidth=2)
ax.axhline(100.0 / player_count, color='gray', linestyle='--', alpha=0.5, label="Equal share")
ax.set_xlabel("Game #")
ax.set_ylabel("Cumulative Win Rate (%)")
ax.set_title("Cumulative Win Rate Over Time")
ax.legend()
ax.set_ylim(0, 100)
ax.grid(True, alpha=0.3)

# 3. Moves per game
ax = axes[0, 2]
bar_colors = [player_colors[w] for w in winners]
ax.bar(games, moves, color=bar_colors, alpha=0.7, width=1.0)
ax.set_xlabel("Game #")
ax.set_ylabel("Moves")
ax.set_title("Moves Per Game (colored by winner)")
ax.axhline(np.mean(moves), color='black', linestyle='--', label=f"Avg: {np.mean(moves):.1f}")
ax.legend()
ax.grid(True, alpha=0.3)

# 4. Average move time per game
ax = axes[1, 0]
ax.plot(games, avg_time, color='#2196F3', linewidth=1, alpha=0.7)
ax.fill_between(games, avg_time, alpha=0.2, color='#2196F3')
ax.set_xlabel("Game #")
ax.set_ylabel("Avg Move Time (ms)")
ax.set_title(f"Average Move Time Per Game (Overall: {np.mean(avg_time):.2f} ms)")
ax.grid(True, alpha=0.3)

# 5. Winner starting HP distribution (only for 2 players)
ax = axes[1, 1]
if player_count == 2:
    winner_hp = [player_hp[0][i] if winners[i] == 0 else player_hp[1][i] for i in range(len(winners))]
    loser_hp = [player_hp[1][i] if winners[i] == 0 else player_hp[0][i] for i in range(len(winners))]
    ax.scatter(winner_hp, loser_hp, c=winners, cmap='RdYlGn_r', alpha=0.6, edgecolors='black', linewidths=0.5)
    ax.plot([30, 80], [30, 80], 'k--', alpha=0.3, label="Equal HP line")
    ax.set_xlabel("Winner Starting HP")
    ax.set_ylabel("Loser Starting HP")
    ax.set_title("Starting HP: Winner vs Loser")
    ax.legend()
    ax.grid(True, alpha=0.3)
else:
    # Win count by player as bar chart
    ax.bar([player_short(i) for i in range(player_count)], player_wins, color=player_colors[:player_count], alpha=0.8)
    ax.set_xlabel("Player")
    ax.set_ylabel("Wins")
    ax.set_title("Total Wins Per Player")
    ax.grid(True, alpha=0.3)

# 6. Starting ATK of winner vs game length
ax = axes[1, 2]
for i in range(player_count):
    mask = winners == i
    if np.any(mask):
        atk_vals = np.array(player_atk[i])[mask]
        ax.scatter(atk_vals, moves[mask], color=player_colors[i], alpha=0.6, label=f"{player_short(i)} wins", edgecolors='black', linewidths=0.5)
ax.set_xlabel("Winner Starting ATK")
ax.set_ylabel("Game Length (Moves)")
ax.set_title("Winner ATK vs Game Length")
ax.legend()
ax.grid(True, alpha=0.3)

plt.tight_layout()
output_path = r"c:\Primora\DATN_PrimoraChronicle\Assets\_Game\Features\AITest\ai_battle_graph.png"
plt.savefig(output_path, dpi=150, bbox_inches='tight')
print(f"Graph saved to: {output_path}")
