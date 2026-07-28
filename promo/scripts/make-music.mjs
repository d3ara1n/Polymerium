// Polymerium promo — procedural score generator.
// Writes 16-bit stereo WAV; no dependencies.
// 120 BPM grid: beat = 0.5s, bar = 2s. All hits land on the video's cut points.
//
// Emotional arc (master): hook 紧张蓄势 → brand 释放温暖 → crafting 轻快俏皮 →
// deploy 流动呼吸（结尾抽空一拍）→ features 明快律动（快照段抽空喘息）→
// mcp 克制悬停 → outro 昂扬收束。D 大调，主旋律贯穿。

import { writeFileSync, mkdirSync } from "node:fs";

const SR = 44100;
const BPM = 120;
const BEAT = 60 / BPM; // 0.5s
const BAR = BEAT * 4; // 2s

/* ── synth primitives ── */

const noteHz = (n) => 440 * Math.pow(2, (n - 69) / 12); // midi → hz

// D major palette
const N = {
  D2: 38, G2: 43, A2: 45, B2: 47,
  D3: 50, E3: 52, Fs3: 54, G3: 55, A3: 57, B3: 59,
  Cs4: 61, D4: 62, E4: 64, Fs4: 66, G4: 67, A4: 69, B4: 71,
  Cs5: 73, D5: 74, E5: 76, Fs5: 78, G5: 79, A5: 81,
};

const CHORDS = {
  D: [N.D3, N.Fs3, N.A3],
  A: [N.A3, N.Cs4, N.E4],
  Bm: [N.B3, N.D4, N.Fs4],
  G: [N.G3, N.B3, N.D4],
  Dadd9: [N.D3, N.Fs3, N.A3, N.E4],
};

function makeCtx(seconds) {
  const len = Math.ceil(seconds * SR);
  return { L: new Float32Array(len), R: new Float32Array(len), len };
}

function add(ctx, t, dur, fn, gain = 1, pan = 0) {
  const start = Math.floor(t * SR);
  const n = Math.floor(dur * SR);
  const pl = Math.sqrt((1 - pan) / 2);
  const pr = Math.sqrt((1 + pan) / 2);
  for (let i = 0; i < n; i++) {
    const idx = start + i;
    if (idx < 0 || idx >= ctx.len) break;
    const s = fn(i / SR) * gain;
    ctx.L[idx] += s * pl * 1.6;
    ctx.R[idx] += s * pr * 1.6;
  }
}

const rnd = (() => {
  let s = 1337;
  return () => ((s = (s * 16807) % 2147483647) / 2147483647) * 2 - 1;
})();

const env = (x, a, d, peak = 1) => {
  if (x < 0) return 0;
  if (x < a) return (x / a) * peak;
  return peak * Math.exp(-(x - a) / d);
};

/* ── instruments ── */

// brighter kick: clicky top, tight sub
const kick = (ctx, t, g = 0.85) =>
  add(ctx, t, 0.2, (x) => {
    const f = 170 * Math.exp(-x * 48) + 50;
    return Math.sin(2 * Math.PI * f * x) * env(x, 0.001, 0.05) + rnd() * env(x, 0.0005, 0.003) * 0.6;
  }, g);

const hat = (ctx, t, g = 0.14, open = false) =>
  add(ctx, t, open ? 0.24 : 0.045, (x) => rnd() * env(x, 0.0008, open ? 0.055 : 0.01), g);

// tambourine-ish shimmer hat for offbeats
const tamb = (ctx, t, g = 0.07) =>
  add(ctx, t, 0.09, (x) => (rnd() * 0.7 + Math.sin(2 * Math.PI * 6200 * x) * 0.3) * env(x, 0.0006, 0.018), g);

const clap = (ctx, t, g = 0.28) =>
  add(ctx, t, 0.16, (x) => {
    const burst = Math.sin(2 * Math.PI * 1600 * x) * 0.3 + rnd();
    return burst * env(x, 0.001, 0.026) * (x < 0.02 ? 1 : 0.65);
  }, g);

// crash cymbal for section impacts
const cymbal = (ctx, t, g = 0.16) =>
  add(ctx, t, 1.6, (x) => rnd() * env(x, 0.001, 0.3) * (0.5 + 0.5 * Math.exp(-x * 2)), g);

// staccato pluck (crafting / playful)
const pluck = (ctx, t, midi, g = 0.22, pan = 0) =>
  add(ctx, t, 0.45, (x) => {
    const f = noteHz(midi);
    const s = Math.sin(2 * Math.PI * f * x) * 0.7 + Math.sin(2 * Math.PI * f * 2.003 * x) * 0.3;
    return s * env(x, 0.004, 0.08);
  }, g, pan);

// lead: brighter, longer, doubled at octave
const lead = (ctx, t, midi, dur = 0.5, g = 0.2, pan = 0) =>
  add(ctx, t, dur + 0.3, (x) => {
    const f = noteHz(midi);
    const vib = 1 + 0.004 * Math.sin(2 * Math.PI * 5.5 * x) * Math.min(1, x * 4);
    const s =
      Math.sin(2 * Math.PI * f * vib * x) * 0.55 +
      Math.sin(2 * Math.PI * f * 2 * vib * x) * 0.25 +
      Math.sin(2 * Math.PI * f * 2.006 * x) * 0.2;
    return s * env(x, 0.006, dur * 0.45);
  }, g, pan);

const bass = (ctx, t, midi, dur = 0.45, g = 0.26) =>
  add(ctx, t, dur, (x) => {
    const f = noteHz(midi);
    const s = Math.sign(Math.sin(2 * Math.PI * f * x)) * 0.5 + Math.sin(2 * Math.PI * f * x) * 0.5;
    return s * env(x, 0.006, dur * 0.5) * 0.85;
  }, g);

const pad = (ctx, t, midis, dur, g = 0.1) =>
  midis.forEach((m, i) =>
    add(ctx, t, dur, (x) => {
      const f = noteHz(m);
      const s = Math.sin(2 * Math.PI * f * x) + Math.sin(2 * Math.PI * f * 1.005 * x);
      return s * 0.5 * env(x, dur * 0.25, dur * 0.22);
    }, g, i % 2 === 0 ? -0.4 : 0.4)
  );

// brighter impact: snap + tight boom, not sub-heavy
const impact = (ctx, t, g = 0.8) =>
  add(ctx, t, 1.2, (x) => {
    const f = 120 * Math.exp(-x * 9) + 42;
    const boom = Math.sin(2 * Math.PI * f * x) * env(x, 0.002, 0.2);
    const snap = rnd() * env(x, 0.001, 0.03) * 0.5;
    return boom + snap;
  }, g);

const riser = (ctx, t, dur, g = 0.16) =>
  add(ctx, t, dur, (x) => {
    const p = x / dur;
    return rnd() * env(x, dur * 0.7, dur * 0.1) * (0.3 + p * 0.7);
  }, g);

const shimmer = (ctx, t, midi, dur, g = 0.09) =>
  add(ctx, t, dur, (x) => {
    const f = noteHz(midi);
    return (Math.sin(2 * Math.PI * f * x) + Math.sin(2 * Math.PI * f * 1.5 * x) * 0.4) *
      env(x, dur * 0.35, dur * 0.25);
  }, g);

/* ── groove helpers ── */

const PROG = ["D", "A", "Bm", "G"]; // D–A–Bm–G
const ROOTS = { D: N.D2, A: N.A2, Bm: N.B2, G: N.G2 };

// driving pop bar: 4-floor kick, offbeat hats+tamb, claps 2&4, pumping bass
function popBar(ctx, bar, { claps = true, drive = true, arpNotes = null } = {}) {
  const t0 = bar * BAR;
  const chord = PROG[bar % 4];
  const root = ROOTS[chord];
  for (let b = 0; b < 4; b++) kick(ctx, t0 + b * BEAT, drive ? 0.85 : 0.6);
  for (let b = 0; b < 8; b++) hat(ctx, t0 + b * BEAT * 0.5, b % 2 ? 0.09 : 0.14, b === 7);
  for (let b = 0; b < 8; b++) b % 2 === 1 && tamb(ctx, t0 + b * BEAT * 0.5);
  if (claps) { clap(ctx, t0 + BEAT); clap(ctx, t0 + 3 * BEAT); }
  // pumping 8th bass with octave bounce
  for (let b = 0; b < 8; b++) bass(ctx, t0 + b * BEAT * 0.5, b % 4 === 3 ? root + 12 : root, 0.2, 0.24);
  pad(ctx, t0, CHORDS[chord], BAR * 0.95, 0.08);
  if (arpNotes) arpNotes.forEach((m, s) => m != null && pluck(ctx, t0 + s * (BEAT / 2), m, 0.13, s % 2 ? 0.45 : -0.45));
}

const ARP_UP = [N.D4, N.Fs4, N.A4, N.D5, N.Cs5, N.A4, N.Fs4, N.A4];

/* ── master arrangement: 78s = 39 bars ── */

function master() {
  const ctx = makeCtx(80);

  // ── bars 0-3 · hook (0-8s): tension, rising, NOT dark
  for (let bar = 0; bar < 4; bar++) {
    const t0 = bar * BAR;
    kick(ctx, t0, 0.55); kick(ctx, t0 + 2 * BEAT, 0.55);
    // staccato eighth pulse on D3 — mid register, restless not gloomy
    for (let b = 0; b < 8; b++) bass(ctx, t0 + b * BEAT * 0.5, N.D3, 0.09, 0.1 + bar * 0.02);
    hat(ctx, t0 + 3.5 * BEAT, 0.06 + bar * 0.015);
  }
  riser(ctx, 4.4, 1.55, 0.15);
  for (let b = 0; b < 16; b++) hat(ctx, 4 + b * 0.125, 0.03 + b * 0.006); // hat crescendo
  impact(ctx, 6.0, 0.85); // collapse
  cymbal(ctx, 6.0, 0.14);
  // release: sudden air + major sparkle
  pad(ctx, 6.15, CHORDS.Dadd9, 3.2, 0.11);
  shimmer(ctx, 6.2, N.D5, 2.2, 0.09);
  shimmer(ctx, 6.9, N.A4, 1.8, 0.07);

  // ── bars 4-6 · brand (8-14s): warm resolve, first melody
  pad(ctx, 8, CHORDS.D, 2.8, 0.12);
  kick(ctx, 8, 0.6);
  lead(ctx, 8.1, N.D5, 0.6, 0.17); // verb 管理
  pad(ctx, 10, CHORDS.A, 2.6, 0.11);
  kick(ctx, 10, 0.6);
  lead(ctx, 9.32, N.Cs5, 0.5, 0.15, 0.25); // verb 打包
  lead(ctx, 10.52, N.A4, 0.5, 0.15, -0.25); // verb 交付
  pad(ctx, 12, CHORDS.G, 2.2, 0.1);
  kick(ctx, 12, 0.55);
  lead(ctx, 11.72, N.B4, 0.45, 0.14, 0.2);
  lead(ctx, 12.9, N.Cs5, 0.5, 0.13, -0.2);

  // ── bars 7-11 · crafting (14-24s): playful bounce
  for (let bar = 7; bar < 12; bar++) {
    const t0 = bar * BAR;
    const root = ROOTS[PROG[bar % 4]];
    for (let b = 0; b < 4; b++) kick(ctx, t0 + b * BEAT, 0.68);
    for (let b = 0; b < 8; b++) hat(ctx, t0 + b * BEAT * 0.5, b % 2 ? 0.08 : 0.12);
    // root–5th bounce bass
    [0, 1, 2, 3].forEach((b) => {
      bass(ctx, t0 + b * BEAT, root, 0.16, 0.2);
      bass(ctx, t0 + b * BEAT + 0.25, root + 7, 0.12, 0.14);
    });
  }
  // playful melody: item drops
  const CRAFT_TUNE = [
    [14.1, N.D5], [14.6, N.Fs5], [15.1, N.A5], [15.6, N.Fs5],
    [16.1, N.D5], [16.6, N.A4], [17.1, N.B4], [17.35, N.Cs5],
  ];
  CRAFT_TUNE.forEach(([t, m], i) => pluck(ctx, t, m, 0.2, i % 2 ? 0.35 : -0.35));
  impact(ctx, 17.6, 0.4); cymbal(ctx, 17.6, 0.1); // craft pop
  pad(ctx, 18, CHORDS.Dadd9, 2.4, 0.09);
  [18.1, 18.85, 19.6].forEach((t, i) => pluck(ctx, t, [N.A5, N.Fs5, N.D5][i], 0.16, 0.3 - i * 0.3));
  clap(ctx, 21.5, 0.2); clap(ctx, 21.75, 0.2); // fill into deploy

  // ── bars 12-16 · deploy (24-34s): flowing, then a breath
  for (let bar = 12; bar < 17; bar++) {
    const t0 = bar * BAR;
    const root = ROOTS[PROG[bar % 4]];
    if (bar !== 15) for (let b = 0; b < 4; b++) kick(ctx, t0 + b * BEAT, 0.62);
    bass(ctx, t0, root, BAR * 0.85, 0.2); // legato long bass
    pad(ctx, t0, CHORDS[PROG[bar % 4]], BAR * 0.96, 0.1);
    ARP_UP.forEach((m, s) => pluck(ctx, t0 + s * (BEAT / 2), m, 0.11, s % 2 ? 0.5 : -0.5));
    if (bar % 2 === 0) { clap(ctx, t0 + BEAT, 0.16); clap(ctx, t0 + 3 * BEAT, 0.16); }
  }
  // bar 15 (30-32s): the breath — no kick, arp thins to quarter notes
  riser(ctx, 32.6, 1.35, 0.17); // into features

  // ── bars 17-31 · features (34-64s): upbeat core, waves per beat
  for (let bar = 17; bar < 32; bar++) {
    const t = bar * BAR;
    const inSnapshotCalm = t >= 54 && t < 58; // beat 05: breather
    if (inSnapshotCalm) {
      const root = ROOTS[PROG[bar % 4]];
      kick(ctx, t, 0.55); kick(ctx, t + 2 * BEAT, 0.55);
      bass(ctx, t, root, BAR * 0.85, 0.2);
      pad(ctx, t, CHORDS[PROG[bar % 4]], BAR * 0.96, 0.11);
      ARP_UP.forEach((m, s) => s % 2 === 0 && pluck(ctx, t + s * (BEAT / 2), m, 0.1, s % 4 ? 0.4 : -0.4));
    } else {
      popBar(ctx, bar, { arpNotes: bar % 2 === 1 ? ARP_UP : null });
    }
  }
  // lead melody over features: two phrases + final ascent
  const PHRASE_A = [[34.1, N.D5], [34.6, N.E5], [35.1, N.Fs5], [35.6, N.E5], [36.1, N.D5], [36.85, N.A4]];
  PHRASE_A.forEach(([t, m]) => lead(ctx, t, m, 0.45, 0.15));
  const PHRASE_B = [[42.1, N.Fs5], [42.6, N.G5], [43.1, N.A5], [43.6, N.G5], [44.1, N.Fs5], [44.85, N.E5]];
  PHRASE_B.forEach(([t, m]) => lead(ctx, t, m, 0.45, 0.15));
  const PHRASE_C = [[49.1, N.A5], [49.6, N.G5], [50.1, N.Fs5], [50.6, N.E5], [51.1, N.Fs5]];
  PHRASE_C.forEach(([t, m]) => lead(ctx, t, m, 0.45, 0.15));
  const ASCENT = [[59.1, N.D5], [59.6, N.E5], [60.1, N.Fs5], [60.6, N.A5]];
  ASCENT.forEach(([t, m]) => lead(ctx, t, m, 0.4, 0.16));
  // fills at beat cuts
  [39, 44, 49, 54, 59].forEach((t) => { clap(ctx, t - 0.25, 0.18); clap(ctx, t - 0.125, 0.14); });
  cymbal(ctx, 34, 0.13); // features entrance crash
  riser(ctx, 63.2, 0.8, 0.14);

  // ── bars 32-35 · mcp (64-72s): restrained hover
  pad(ctx, 64, CHORDS.Bm, 3.8, 0.09);
  for (let bar = 32; bar < 36; bar++) {
    const t0 = bar * BAR;
    kick(ctx, t0, 0.6); kick(ctx, t0 + 2 * BEAT, 0.6);
    bass(ctx, t0, N.D3, 0.3, 0.18); bass(ctx, t0 + 2.5 * BEAT, N.D3, 0.2, 0.15);
    hat(ctx, t0 + BEAT, 0.09); hat(ctx, t0 + 3 * BEAT, 0.09);
    // techy echoes, alternating pan
    pluck(ctx, t0 + 3.5 * BEAT, [N.D5, N.Fs5, N.A4, N.Cs5][bar % 4], 0.12, bar % 2 ? 0.5 : -0.5);
  }
  riser(ctx, 70.6, 1.35, 0.17);

  // ── bars 36-38 · outro (72-78s): triumphant resolve
  impact(ctx, 72, 0.5); cymbal(ctx, 72, 0.15);
  pad(ctx, 72, CHORDS.Dadd9, 4.6, 0.14);
  lead(ctx, 72.1, N.Fs5, 0.7, 0.2);
  lead(ctx, 73.1, N.E5, 0.7, 0.18, 0.25);
  kick(ctx, 74, 0.65);
  lead(ctx, 74.1, N.D5, 1.6, 0.22, -0.15); // long resolve
  pad(ctx, 74.5, CHORDS.G, 1.8, 0.09);
  pad(ctx, 76, CHORDS.D, 2.4, 0.1);
  kick(ctx, 76, 0.55);
  shimmer(ctx, 76.4, N.D5, 1.6, 0.07);

  return ctx;
}

/* ── teaser arrangement: 15s ── */
function teaser() {
  const ctx = makeCtx(15.5);
  kick(ctx, 0, 0.6);
  for (let b = 0; b < 8; b++) bass(ctx, b * 0.125, N.D3, 0.08, 0.1);
  riser(ctx, 0.1, 0.95, 0.14);
  impact(ctx, 1.1, 0.85); cymbal(ctx, 1.1, 0.13);
  pad(ctx, 1.25, CHORDS.Dadd9, 1.2, 0.1);
  shimmer(ctx, 1.3, N.D5, 1.2, 0.08);
  // brand (2.5-5.5s)
  pad(ctx, 2.5, CHORDS.D, 2.8, 0.12);
  kick(ctx, 2.5, 0.6); kick(ctx, 4.5, 0.6);
  lead(ctx, 2.6, N.D5, 0.55, 0.16);
  lead(ctx, 3.8, N.A4, 0.5, 0.14, 0.3);
  lead(ctx, 4.6, N.B4, 0.45, 0.13, -0.3);
  // flash groove (5.5-12s)
  for (let bar = 0; bar < 3; bar++) {
    const t0 = 5.5 + bar * BAR;
    for (let b = 0; b < 4; b++) kick(ctx, t0 + b * BEAT, 0.8);
    for (let b = 0; b < 8; b++) hat(ctx, t0 + b * BEAT * 0.5, b % 2 ? 0.08 : 0.13);
    for (let b = 0; b < 8; b++) b % 2 === 1 && tamb(ctx, t0 + b * BEAT * 0.5, 0.06);
    clap(ctx, t0 + BEAT, 0.24); clap(ctx, t0 + 3 * BEAT, 0.24);
    const root = [N.D2, N.A2, N.B2][bar];
    for (let b = 0; b < 8; b++) bass(ctx, t0 + b * BEAT * 0.5, b % 4 === 3 ? root + 12 : root, 0.18, 0.22);
    pad(ctx, t0, CHORDS[["D", "A", "Bm"][bar]], BAR * 0.95, 0.08);
  }
  [[5.6, N.D5], [6.1, N.E5], [6.6, N.Fs5], [7.6, N.A5], [8.1, N.G5], [8.6, N.Fs5]].forEach(([t, m]) =>
    lead(ctx, t, m, 0.4, 0.14));
  riser(ctx, 11.1, 0.85, 0.15);
  // outro (12s)
  impact(ctx, 12, 0.5); cymbal(ctx, 12, 0.14);
  pad(ctx, 12, CHORDS.Dadd9, 3.2, 0.14);
  lead(ctx, 12.1, N.Fs5, 0.6, 0.19);
  lead(ctx, 13.1, N.D5, 1.2, 0.2, -0.2);
  shimmer(ctx, 13.8, N.A4, 1.2, 0.07);
  return ctx;
}

/* ── short arrangement: 30s ── */
function short() {
  const ctx = makeCtx(30.5);
  // hook (0-4s): tension + impact at 2.2s
  kick(ctx, 0, 0.6); kick(ctx, 1, 0.6);
  for (let b = 0; b < 16; b++) bass(ctx, b * 0.125, N.D3, 0.08, 0.09 + b * 0.004);
  riser(ctx, 0.6, 1.5, 0.14);
  impact(ctx, 2.2, 0.85); cymbal(ctx, 2.2, 0.13);
  pad(ctx, 2.35, CHORDS.Dadd9, 1.5, 0.1);
  shimmer(ctx, 2.4, N.D5, 1.4, 0.08);
  // craft (4-12s): playful
  for (let bar = 0; bar < 4; bar++) {
    const t0 = 4 + bar * BAR;
    const root = ROOTS[PROG[bar % 4]];
    for (let b = 0; b < 4; b++) kick(ctx, t0 + b * BEAT, 0.68);
    for (let b = 0; b < 8; b++) hat(ctx, t0 + b * BEAT * 0.5, b % 2 ? 0.07 : 0.11);
    [0, 1, 2, 3].forEach((b) => {
      bass(ctx, t0 + b * BEAT, root, 0.15, 0.18);
      bass(ctx, t0 + b * BEAT + 0.25, root + 7, 0.11, 0.13);
    });
  }
  [[4.1, N.D5], [4.6, N.Fs5], [5.1, N.A5], [5.6, N.Fs5], [6.1, N.D5], [6.6, N.A4], [7.0, N.B4]].forEach(([t, m], i) =>
    pluck(ctx, t, m, 0.18, i % 2 ? 0.35 : -0.35));
  impact(ctx, 7.17, 0.35); cymbal(ctx, 7.17, 0.09);
  pad(ctx, 8, CHORDS.Dadd9, 2.2, 0.09);
  // deploy (12-18s): flowing arp
  for (let bar = 0; bar < 3; bar++) {
    const t0 = 12 + bar * BAR;
    for (let b = 0; b < 4; b++) kick(ctx, t0 + b * BEAT, 0.6);
    bass(ctx, t0, ROOTS[PROG[bar % 4]], BAR * 0.85, 0.19);
    pad(ctx, t0, CHORDS[PROG[bar % 4]], BAR * 0.96, 0.09);
    ARP_UP.forEach((m, s) => pluck(ctx, t0 + s * (BEAT / 2), m, 0.11, s % 2 ? 0.45 : -0.45));
    clap(ctx, t0 + BEAT, 0.16); clap(ctx, t0 + 3 * BEAT, 0.16);
  }
  riser(ctx, 16.8, 1.1, 0.15);
  // flash (18-23.5s): driving
  for (let bar = 0; bar < 3; bar++) {
    const t0 = 18 + bar * BAR;
    for (let b = 0; b < 4; b++) kick(ctx, t0 + b * BEAT, 0.82);
    for (let b = 0; b < 8; b++) hat(ctx, t0 + b * BEAT * 0.5, b % 2 ? 0.09 : 0.14);
    for (let b = 0; b < 8; b++) b % 2 === 1 && tamb(ctx, t0 + b * BEAT * 0.5, 0.06);
    clap(ctx, t0 + BEAT, 0.24); clap(ctx, t0 + 3 * BEAT, 0.24);
    const root = [N.D2, N.A2, N.B2][bar];
    for (let b = 0; b < 8; b++) bass(ctx, t0 + b * BEAT * 0.5, b % 4 === 3 ? root + 12 : root, 0.18, 0.22);
    pad(ctx, t0, CHORDS[["D", "A", "Bm"][bar]], BAR * 0.95, 0.08);
  }
  [[18.1, N.D5], [18.6, N.E5], [19.1, N.Fs5], [20.1, N.A5], [20.6, N.G5], [21.1, N.Fs5]].forEach(([t, m]) =>
    lead(ctx, t, m, 0.4, 0.14));
  riser(ctx, 22.6, 0.85, 0.15);
  // outro (23.5-30s)
  impact(ctx, 23.5, 0.5); cymbal(ctx, 23.5, 0.14);
  pad(ctx, 23.5, CHORDS.Dadd9, 4.4, 0.14);
  lead(ctx, 23.6, N.Fs5, 0.7, 0.19);
  lead(ctx, 24.6, N.E5, 0.6, 0.16, 0.25);
  lead(ctx, 25.6, N.D5, 1.4, 0.2, -0.15);
  kick(ctx, 25.5, 0.6); kick(ctx, 27.5, 0.55);
  shimmer(ctx, 27.8, N.D5, 1.5, 0.07);
  return ctx;
}

/* ── WAV writer ── */

function toWav(ctx) {
  const n = ctx.len;
  const buf = Buffer.alloc(44 + n * 4);
  buf.write("RIFF", 0); buf.writeUInt32LE(36 + n * 4, 4); buf.write("WAVE", 8);
  buf.write("fmt ", 12); buf.writeUInt32LE(16, 16); buf.writeUInt16LE(1, 20); buf.writeUInt16LE(2, 22);
  buf.writeUInt32LE(SR, 24); buf.writeUInt32LE(SR * 4, 28); buf.writeUInt16LE(4, 32); buf.writeUInt16LE(16, 34);
  buf.write("data", 36); buf.writeUInt32LE(n * 4, 40);
  for (let i = 0; i < n; i++) {
    const l = Math.max(-1, Math.min(1, Math.tanh(ctx.L[i] * 0.9)));
    const r = Math.max(-1, Math.min(1, Math.tanh(ctx.R[i] * 0.9)));
    buf.writeInt16LE(Math.round(l * 32767), 44 + i * 4);
    buf.writeInt16LE(Math.round(r * 32767), 44 + i * 4 + 2);
  }
  return buf;
}

mkdirSync("public", { recursive: true });
writeFileSync("public/music-master.wav", toWav(master()));
writeFileSync("public/music-teaser.wav", toWav(teaser()));
writeFileSync("public/music-short.wav", toWav(short()));
console.log("music-master/teaser/short.wav written");
