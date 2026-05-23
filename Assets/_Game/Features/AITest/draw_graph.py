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
p0_wins = int(re.search(r"\*\*Player 0.*?Wins:\*\* (\d+)", content).group(1))
p1_wins = int(re.search(r"\*\*Player 1.*?Wins:\*\* (\d+)", content).group(1))

# Parse table rows
rows = re.findall(
    r"\| (\d+) \| (\d+) \| (\d+) \| (\d+) \| (.+?) \| (\d+) \| (\d+) \| (\d+) \| (.+?) \| (\d+) \| (\d+) \| ([\d.]+) \|",
    content
)

games, p0_hp, p0_atk, p0_spd, p1_hp, p1_atk, p1_spd = [], [], [], [], [], [], []
winners, moves, avg_time = [], [], []

for row in rows:
    games.append(int(row[0]))
    p0_hp.append(int(row[1]))
    p0_atk.append(int(row[2]))
    p0_spd.append(int(row[3]))
    p1_hp.append(int(row[5]))
    p1_atk.append(int(row[6]))
    p1_spd.append(int(row[7]))
    winners.append(int(row[9]))
    moves.append(int(row[10]))
    avg_time.append(float(row[11]))

games = np.array(games)
winners = np.array(winners)
moves = np.array(moves)
avg_time = np.array(avg_time)

fig, axes = plt.subplots(2, 3, figsize=(18, 10))
fig.suptitle(f"AI Battle Results — {total_games} Games | P0 Wins: {p0_wins} ({p0_wins/total_games*100:.1f}%) | P1 Wins: {p1_wins} ({p1_wins/total_games*100:.1f}%)", fontsize=14, fontweight='bold')

# 1. Win rate pie chart
ax = axes[0, 0]
ax.pie([p0_wins, p1_wins], labels=["Player 0", "Player 1"], autopct='%1.1f%%',
       colors=['#4CAF50', '#F44336'], startangle=90)
ax.set_title("Win Rate")

# 2. Cumulative win rate over games
ax = axes[0, 1]
cum_p0 = np.cumsum(winners == 0)
cum_p1 = np.cumsum(winners == 1)
ax.plot(games, cum_p0 / games * 100, label="Player 0", color='#4CAF50', linewidth=2)
ax.plot(games, cum_p1 / games * 100, label="Player 1", color='#F44336', linewidth=2)
ax.axhline(50, color='gray', linestyle='--', alpha=0.5)
ax.set_xlabel("Game #")
ax.set_ylabel("Cumulative Win Rate (%)")
ax.set_title("Cumulative Win Rate Over Time")
ax.legend()
ax.set_ylim(0, 100)
ax.grid(True, alpha=0.3)

# 3. Moves per game
ax = axes[0, 2]
colors = ['#4CAF50' if w == 0 else '#F44336' for w in winners]
ax.bar(games, moves, color=colors, alpha=0.7, width=1.0)
ax.set_xlabel("Game #")
ax.set_ylabel("Moves")
ax.set_title("Moves Per Game (Green=P0 win, Red=P1 win)")
ax.axhline(np.mean(moves), color='blue', linestyle='--', label=f"Avg: {np.mean(moves):.1f}")
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

# 5. HP comparison: winner vs loser
ax = axes[1, 1]
winner_hp = [p0_hp[i] if winners[i] == 0 else p1_hp[i] for i in range(len(winners))]
loser_hp = [p1_hp[i] if winners[i] == 0 else p0_hp[i] for i in range(len(winners))]
ax.scatter(winner_hp, loser_hp, c=winners, cmap='RdYlGn_r', alpha=0.6, edgecolors='black', linewidths=0.5)
ax.plot([30, 80], [30, 80], 'k--', alpha=0.3, label="Equal HP line")
ax.set_xlabel("Winner Starting HP")
ax.set_ylabel("Loser Starting HP")
ax.set_title("Starting HP: Winner vs Loser")
ax.legend()
ax.grid(True, alpha=0.3)

# 6. Attack advantage vs outcome
ax = axes[1, 2]
atk_diff = np.array(p0_atk) - np.array(p1_atk)
p0_win_mask = winners == 0
ax.scatter(atk_diff[p0_win_mask], moves[p0_win_mask], color='#4CAF50', alpha=0.6, label="P0 wins", edgecolors='black', linewidths=0.5)
ax.scatter(atk_diff[~p0_win_mask], moves[~p0_win_mask], color='#F44336', alpha=0.6, label="P1 wins", edgecolors='black', linewidths=0.5)
ax.axvline(0, color='gray', linestyle='--', alpha=0.5)
ax.set_xlabel("P0 Attack - P1 Attack")
ax.set_ylabel("Game Length (Moves)")
ax.set_title("Attack Advantage vs Game Length")
ax.legend()
ax.grid(True, alpha=0.3)

plt.tight_layout()
output_path = r"c:\Primora\DATN_PrimoraChronicle\Assets\_Game\Features\AITest\ai_battle_graph.png"
plt.savefig(output_path, dpi=150, bbox_inches='tight')
print(f"Graph saved to: {output_path}")
