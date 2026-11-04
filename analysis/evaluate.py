"""M5 evaluation: turns the simulator's CSV (analysis/out/) into docs/figures/*.png and docs/report.md.

Usage: python3 analysis/evaluate.py [--data analysis/out] [--docs docs]
The report text lives in analysis/report_template.md; every number in it is computed here.
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from string import Template

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402
import pandas as pd  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent

# Reference categorical palette (dataviz skill), fixed order; grey is a neutral reference, not a series slot.
BLUE, ORANGE, AQUA = "#2a78d6", "#eb6834", "#1baf7a"
GREY, INK, INK2, GRID = "#8a8985", "#0b0b0b", "#52514e", "#e4e3df"
CHALLENGES = ["Relaxed", "Standard", "Hard"]
TARGETS = {"Relaxed": 0.85, "Standard": 0.70, "Hard": 0.55}
POPS = ["faster", "default", "slower"]
POP_LABEL = {"faster": "Faster players (×0.75)", "default": "Default population", "slower": "Slower players (×1.4)"}
FIXED_LEAD = 420


def style() -> None:
    plt.rcParams.update({
        "figure.dpi": 100, "savefig.dpi": 200, "font.size": 9, "axes.titlesize": 10, "axes.titleweight": "bold",
        "axes.titlelocation": "left", "axes.edgecolor": GRID, "axes.labelcolor": INK2, "xtick.color": INK2,
        "ytick.color": INK2, "axes.grid": True, "grid.color": GRID, "grid.linewidth": 0.6, "axes.spines.top": False,
        "axes.spines.right": False, "legend.frameon": False, "text.color": INK, "figure.facecolor": "white",
        "axes.facecolor": "white", "lines.linewidth": 2, "axes.axisbelow": True,
    })


def load(data: Path) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    runs = pd.read_csv(data / "runs.csv")
    players = pd.read_csv(data / "players.csv")
    attacks = pd.read_csv(data / "attacks.csv", dtype={"run": "int16", "player": "int16", "attack": "int16", "control": "int8",
                                                       "lead_ms": "int16", "success": "int8"})
    attacks = attacks.merge(players[["run", "player", "true_median_ms"]], on=["run", "player"])
    attacks["err_ms"] = (attacks["est_median_ms"] - attacks["true_median_ms"]).abs()
    attacks["covered"] = ((attacks["est_low_ms"] <= attacks["true_median_ms"]) & (attacks["true_median_ms"] <= attacks["est_high_ms"])).astype("int8")
    return runs, players, attacks


def per_player(runs: pd.DataFrame, attacks: pd.DataFrame) -> pd.DataFrame:
    ctl = attacks[attacks["control"] == 1].groupby(["run", "player"]).agg(run_success=("success", "mean"), mean_lead=("lead_ms", "mean"),
                                                                          share_at_cap=("lead_ms", lambda s: (s >= 800).mean()))
    return ctl.reset_index().merge(runs, on="run")


def condition(r: pd.Series) -> str:
    if r["learn"] == 0:
        return "prior"
    return ("both" if r["prior"] == "population" else "data") + ("_cal" if r["calibration_attacks"] > 0 else "_nocal")


def error_curves(runs: pd.DataFrame, attacks: pd.DataFrame) -> pd.DataFrame:
    lr = runs[runs["experiment"] == "learning"].copy()
    lr["condition"] = lr.apply(condition, axis=1)
    a = attacks[attacks["run"].isin(lr["run"])]
    g = a.groupby(["run", "attack"])["err_ms"]
    curves = pd.DataFrame({"median": g.median(), "q25": g.quantile(0.25), "q75": g.quantile(0.75),
                           "within40": a.assign(w=a["err_ms"] <= 40).groupby(["run", "attack"])["w"].mean(),
                           "coverage": a.groupby(["run", "attack"])["covered"].mean()}).reset_index()
    return curves.merge(lr[["run", "population", "condition"]], on="run")


COND_STYLE = {
    "prior": ("Population prior only", GREY, "-"),
    "data_cal": ("Player data only", ORANGE, "-"),
    "both_cal": ("Prior + player data", BLUE, "-"),
    "data_nocal": ("Player data only", ORANGE, "-"),
    "both_nocal": ("Prior + player data (shipped)", BLUE, "-"),
}


def plot_curves(ax, curves: pd.DataFrame, pop: str, conds: list[str], label_end: bool = True) -> None:
    for c in conds:
        d = curves[(curves["population"] == pop) & (curves["condition"] == c)].sort_values("attack")
        name, color, ls = COND_STYLE[c]
        ax.fill_between(d["attack"], d["q25"], d["q75"], color=color, alpha=0.07 if c == "prior" else 0.13, linewidth=0)
        ax.plot(d["attack"], d["median"], color=color, ls=ls, label=name)
    ax.axhline(40, color=INK2, lw=0.8, ls=(0, (3, 3)))
    ax.axvline(15, color=INK2, lw=0.8, ls=(0, (1, 2)))
    ax.set_xscale("log")
    ax.set_xlim(1, 60)
    ax.set_xticks([1, 2, 5, 10, 15, 30, 60], ["1", "2", "5", "10", "15", "30", "60"])
    ax.minorticks_off()
    ax.set_xlabel("Attacks seen (log scale)")


def fig_learning(curves: pd.DataFrame, out: Path, n_players: int) -> None:
    fig, axes = plt.subplots(1, 2, figsize=(7.6, 3.1), sharey=True)
    for ax, cal, title in [(axes[0], "_nocal", "Shipped: control from attack 1"),
                           (axes[1], "_cal", "For comparison: 12 Psi calibration attacks first")]:
        plot_curves(ax, curves, "default", ["prior", "data" + cal, "both" + cal])
        ax.set_title(title, fontsize=9)
    axes[0].set_ylabel("|estimated − true median| (ms)")
    axes[0].set_ylim(0, 160)
    axes[0].text(15.8, 150, "attack 15", color=INK2, fontsize=7.5, va="top")
    axes[0].text(59, 43, "40 ms", color=INK2, fontsize=7.5, ha="right")
    h, l = axes[0].get_legend_handles_labels()
    fig.legend(h, l, loc="lower center", ncol=3, bbox_to_anchor=(0.5, -0.02))
    fig.suptitle(f"Fig. 1  Estimate error against attacks seen (median and IQR, {n_players:,} players)", x=0.01, ha="left", fontsize=10, fontweight="bold")
    fig.tight_layout(rect=(0, 0.08, 1, 1))
    fig.savefig(out / "fig1_learning.png", bbox_inches="tight")
    plt.close(fig)


def fig_shifted(curves: pd.DataFrame, out: Path) -> None:
    fig, axes = plt.subplots(1, 3, figsize=(7.6, 2.9), sharey=True)
    for ax, pop in zip(axes, POPS):
        plot_curves(ax, curves, pop, ["prior", "data_nocal", "both_nocal"])
        ax.set_title(POP_LABEL[pop], fontsize=9)
    axes[0].set_ylabel("|estimated − true median| (ms)")
    axes[0].set_ylim(0, 200)
    h, l = axes[0].get_legend_handles_labels()
    fig.legend(h, l, loc="lower center", ncol=3, bbox_to_anchor=(0.5, -0.02))
    fig.suptitle("Fig. 3  Estimate error on populations the prior was not fitted to", x=0.01, ha="left", fontsize=10, fontweight="bold")
    fig.tight_layout(rect=(0, 0.09, 1, 1))
    fig.savefig(out / "fig3_shifted.png", bbox_inches="tight")
    plt.close(fig)


def fig_success(pp: pd.DataFrame, out: Path) -> None:
    d = pp[pp["experiment"] == "challenge"]
    fig, axes = plt.subplots(1, 3, figsize=(7.6, 2.8), sharey=True)
    bins = np.arange(-0.5, 61.5, 2) / 60
    for ax, ch in zip(axes, CHALLENGES):
        t = TARGETS[ch]
        ax.axvspan(t - 0.1, t + 0.1, color=BLUE, alpha=0.08, linewidth=0)
        for timing, color, name in [("fixed", GREY, f"Fixed {FIXED_LEAD} ms"), ("adaptive", BLUE, "Adaptive")]:
            x = d[(d["challenge"] == ch) & (d["timing"] == timing)]["run_success"]
            ax.hist(x, bins=bins, color=color, alpha=0.85 if timing == "adaptive" else 0.55, label=name,
                    edgecolor="white", linewidth=0.6)
        ax.axvline(t, color=INK, lw=1)
        ax.set_title(f"{ch} (target {t:.2f})", fontsize=9)
        ax.set_xlim(0, 1)
        ax.set_xlabel("Success rate over 60 run attacks")
    axes[0].set_ylabel("Players")
    h, l = axes[0].get_legend_handles_labels()
    fig.legend(h[::-1], l[::-1] + [], loc="lower center", ncol=2, bbox_to_anchor=(0.5, -0.02))
    fig.suptitle("Fig. 2  Success rate per player, adaptive against fixed timing (shaded: target ±10 points)",
                 x=0.01, ha="left", fontsize=10, fontweight="bold")
    fig.tight_layout(rect=(0, 0.09, 1, 1))
    fig.savefig(out / "fig2_success.png", bbox_inches="tight")
    plt.close(fig)


def reliability(attacks: pd.DataFrame, run_ids, bins=np.linspace(0, 1, 21), min_n: int = 500) -> pd.DataFrame:
    """Every attack (early attacks, under a wide belief, spread the predictions; later ones cluster at the targets)."""
    a = attacks[attacks["run"].isin(run_ids)]
    b = pd.cut(a["predicted"], bins, include_lowest=True)
    g = a.groupby(b, observed=True).agg(pred=("predicted", "mean"), obs=("success", "mean"), n=("success", "size"))
    return g[g["n"] >= min_n].reset_index(drop=True)


def ece(rel: pd.DataFrame) -> float:
    return float((rel["n"] * (rel["pred"] - rel["obs"]).abs()).sum() / rel["n"].sum())


def fig_wrong_model(runs: pd.DataFrame, attacks: pd.DataFrame, pp: pd.DataFrame, out: Path) -> dict:
    fig, (a1, a2) = plt.subplots(1, 2, figsize=(7.6, 3.3), gridspec_kw={"width_ratios": [1, 1.15]})
    stats = {}
    a1.plot([0.3, 0.9], [0.3, 0.9], color=INK2, lw=0.8, ls=(0, (3, 3)))
    for model, color, name in [("lognormal", BLUE, "Log-normal bots (the model)"), ("exgaussian", ORANGE, "Ex-Gaussian bots")]:
        ids = runs[(runs["model"] == model) & runs["experiment"].isin(["challenge", "wrong_model"]) & (runs["timing"] == "adaptive")]["run"]
        rel = reliability(attacks, ids)
        stats[model] = rel
        a1.plot(rel["pred"], rel["obs"], color=color, marker="o", ms=4.5, mec="white", mew=1, lw=1.5, label=name)
    a1.set_xlim(0.3, 0.9); a1.set_ylim(0.3, 0.9); a1.set_aspect("equal")
    a1.set_xlabel("Predicted success (belief average)")
    a1.set_ylabel("Observed success")
    a1.set_title("Predicted against observed (all attacks)", fontsize=9)
    a1.legend(loc="upper left", fontsize=8)

    d = pp[pp["experiment"].isin(["challenge", "wrong_model"]) & (pp["timing"] == "adaptive")]
    for i, ch in enumerate(CHALLENGES):
        t = TARGETS[ch]
        a2.plot([i - 0.38, i + 0.38], [t, t], color=INK, lw=1)
        for j, (model, color) in enumerate([("lognormal", BLUE), ("exgaussian", ORANGE)]):
            x = d[(d["challenge"] == ch) & (d["model"] == model)]["run_success"]
            pos = i + (j - 0.5) * 0.36
            q10, q25, q50, q75, q90 = np.quantile(x, [0.1, 0.25, 0.5, 0.75, 0.9])
            a2.plot([pos, pos], [q10, q90], color=color, lw=1.2)
            a2.plot([pos, pos], [q25, q75], color=color, lw=5, solid_capstyle="round")
            a2.plot([pos], [x.mean()], marker="o", color="white", mec=color, mew=1.5, ms=5)
    a2.set_xticks(range(3), [f"{c}\n(target {TARGETS[c]:.2f})" for c in CHALLENGES])
    a2.set_ylim(0.3, 1.0)
    a2.set_ylabel("Run success rate per player")
    a2.set_title("Success per player (bar: IQR, line: 10–90%, dot: mean)", fontsize=9)
    a2.grid(axis="x", visible=False)
    fig.suptitle("Fig. 4  A wrong model: bots drawing reaction times from an ex-Gaussian", x=0.01, ha="left", fontsize=10, fontweight="bold")
    fig.tight_layout()
    fig.savefig(out / "fig4_wrong_model.png", bbox_inches="tight")
    plt.close(fig)
    return stats


def fig_leads(pp: pd.DataFrame, players: pd.DataFrame, out: Path) -> None:
    d = pp[(pp["experiment"] == "learning") & (pp["condition"] == "both_nocal")].merge(players, on=["run", "player"])
    fig, ax = plt.subplots(figsize=(7.6, 3.0))
    for pop, color in zip(POPS, [AQUA, BLUE, ORANGE]):
        x = d[d["population"] == pop]
        ax.scatter(x["true_median_ms"], x["mean_lead"], s=5, color=color, alpha=0.45, linewidths=0, label=POP_LABEL[pop])
    for y, label in [(800, "lead cap 800 ms"), (200, "lead floor 200 ms"), (FIXED_LEAD, f"fixed timing {FIXED_LEAD} ms")]:
        ax.axhline(y, color=INK2, lw=0.8, ls=(0, (3, 3)))
        ax.text(865, y + 8, label, fontsize=7.5, color=INK2, ha="right", va="bottom")
    ax.set_xlabel("True median reaction time (ms)")
    ax.set_ylabel("Mean control lead (ms)")
    ax.set_xlim(150, 870); ax.set_ylim(170, 850)
    h, l = ax.get_legend_handles_labels()
    leg = fig.legend(h, l, loc="lower center", ncol=3, bbox_to_anchor=(0.5, -0.02), fontsize=8.5, markerscale=3)
    for h in leg.legend_handles:
        h.set_alpha(1)
    ax.set_title("Fig. 5  The lead the director settles on, per player (Standard, 0.70)", fontsize=10)
    fig.tight_layout(rect=(0, 0.08, 1, 1))
    fig.savefig(out / "fig5_leads.png", bbox_inches="tight")
    plt.close(fig)


def pct(x: float) -> str:
    return f"{100 * x:.1f}%"


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", default=str(ROOT / "analysis" / "out"))
    ap.add_argument("--docs", default=str(ROOT / "docs"))
    args = ap.parse_args()
    data, docs = Path(args.data), Path(args.docs)
    figs = docs / "figures"
    figs.mkdir(parents=True, exist_ok=True)
    style()

    runs, players, attacks = load(data)
    runs["condition"] = runs.apply(condition, axis=1)
    pp = per_player(runs, attacks)
    curves = error_curves(runs, attacks)

    fig_learning(curves, figs, int((players["run"] == 0).sum()))
    fig_success(pp, figs)
    fig_shifted(curves, figs)
    rel = fig_wrong_model(runs, attacks, pp, figs)
    fig_leads(pp, players, figs)

    v: dict[str, str] = {}

    def cv(pop: str, cond: str, attack: int, col: str) -> float:
        r = curves[(curves["population"] == pop) & (curves["condition"] == cond) & (curves["attack"] == attack)]
        return float(r[col].iloc[0])

    # 1. Learning speed.
    for cond in ["prior", "data_cal", "data_nocal", "both_cal", "both_nocal"]:
        for k in [1, 5, 15, 30, 60]:
            v[f"err_{cond}_{k}"] = f"{cv('default', cond, k, 'median'):.0f}"
        v[f"w40_{cond}_15"] = pct(cv("default", cond, 15, "within40"))
        v[f"w40_{cond}_60"] = pct(cv("default", cond, 60, "within40"))
    for k in [15, 60]:
        v[f"cov_both_nocal_{k}"] = pct(cv("default", "both_nocal", k, "coverage"))
    for pop in POPS:
        for cond in ["prior", "data_nocal", "both_nocal"]:
            for k in [15, 60]:
                v[f"err_{pop}_{cond}_{k}"] = f"{cv(pop, cond, k, 'median'):.0f}"
                v[f"w40_{pop}_{cond}_{k}"] = pct(cv(pop, cond, k, "within40"))
    # First attack at which the median error falls below 40 ms.
    for pop in POPS:
        for cond in ["data_cal", "both_cal", "data_nocal", "both_nocal"]:
            d = curves[(curves["population"] == pop) & (curves["condition"] == cond)].sort_values("attack")
            hit = d[d["median"] <= 40]["attack"]
            v[f"first40_{pop}_{cond}"] = str(int(hit.iloc[0])) if len(hit) else "never"

    # 2. Challenge.
    rows = []
    for exp, model in [("challenge", "lognormal"), ("wrong_model", "exgaussian")]:
        for ch in CHALLENGES:
            t = TARGETS[ch]
            for timing in ["adaptive", "fixed"]:
                x = pp[(pp["experiment"] == exp) & (pp["challenge"] == ch) & (pp["timing"] == timing)]["run_success"]
                key = f"{model}_{ch}_{timing}"
                v[f"on_{key}"] = pct(float(((x - t).abs() <= 0.1 + 1e-9).mean()))
                v[f"mean_{key}"] = f"{x.mean():.3f}"
                v[f"sd_{key}"] = f"{x.std(ddof=0):.3f}"
            v[f"floor_{ch}"] = f"{math.sqrt(t * (1 - t) / 60):.3f}"
    # 3. Shifted populations: success at Standard.
    for pop in POPS:
        for cond, timing, label in [("both_nocal", "adaptive", "both"), ("data_nocal", "adaptive", "data"), ("prior", "adaptive", "prior")]:
            x = pp[(pp["experiment"] == "learning") & (pp["population"] == pop) & (pp["condition"] == cond)]
            v[f"on_{pop}_{label}"] = pct(float(((x["run_success"] - 0.7).abs() <= 0.1 + 1e-9).mean()))
            v[f"mean_{pop}_{label}"] = f"{x['run_success'].mean():.3f}"
        exp = "challenge" if pop == "default" else "shifted"
        x = pp[(pp["experiment"] == exp) & (pp["population"] == pop) & (pp["challenge"] == "Standard") & (pp["timing"] == "fixed")]
        v[f"on_{pop}_fixed"] = pct(float(((x["run_success"] - 0.7).abs() <= 0.1 + 1e-9).mean()))
        v[f"mean_{pop}_fixed"] = f"{x['run_success'].mean():.3f}"
    slow = pp[(pp["experiment"] == "learning") & (pp["population"] == "slower") & (pp["condition"] == "both_nocal")].merge(
        players, on=["run", "player"])
    capped = slow[slow["share_at_cap"] >= 0.5]
    v["slow_capped_share"] = pct(len(capped) / len(slow))
    v["slow_capped_min_median"] = f"{capped['true_median_ms'].min():.0f}" if len(capped) else "-"
    v["slow_capped_mean_success"] = f"{capped['run_success'].mean():.3f}" if len(capped) else "-"
    uncapped = slow[slow["share_at_cap"] < 0.5]
    v["slow_uncapped_on"] = pct(float(((uncapped["run_success"] - 0.7).abs() <= 0.1 + 1e-9).mean()))

    # 4. Wrong model.
    for model in ["lognormal", "exgaussian"]:
        v[f"ece_{model}"] = f"{100 * ece(rel[model]):.1f}"
        r = rel[model]
        v[f"maxgap_{model}"] = f"{100 * (r['pred'] - r['obs']).abs().max():.1f}"
    eg_runs = runs[(runs["model"] == "exgaussian") & (runs["timing"] == "adaptive") & (runs["challenge"] == "Standard")]["run"]
    a = attacks[attacks["run"].isin(eg_runs) & (attacks["attack"] == 60)]
    bias = a["est_median_ms"] - a["true_median_ms"]
    v["eg_bias_median"] = f"{bias.median():+.0f}"
    v["eg_err_60"] = f"{a['err_ms'].median():.0f}"
    v["eg_cov_60"] = pct(float(a["covered"].mean()))
    a15 = attacks[attacks["run"].isin(eg_runs) & (attacks["attack"] == 15)]
    v["eg_w40_15"] = pct(float((a15["err_ms"] <= 40).mean()))
    eg = players[players["run"].isin(eg_runs)]
    v["eg_tau_share"] = f"{(eg['tau_ms'] / eg['true_median_ms']).median():.2f}"

    v["players"] = f"{players[players['run'] == 0].shape[0]:,}"
    v["runs"] = str(len(runs))
    v["attacks_total"] = f"{len(attacks):,}"

    (data / "summary.json").write_text(json.dumps(v, indent=1, sort_keys=True))
    template = Template((ROOT / "analysis" / "report_template.md").read_text(encoding="utf-8"))
    (docs / "report.md").write_text(template.substitute(v), encoding="utf-8")
    print(f"report: {docs / 'report.md'}; figures: {', '.join(sorted(p.name for p in figs.glob('*.png')))}")


if __name__ == "__main__":
    main()
