// Shared beat grid for picture and score. One beat is exactly 28 frames at 60 fps
// (≈128.57 BPM), so every cue lands on a whole frame.
export const FPS = 60;
export const BEAT = 28 / FPS;
export const BPM = 60 / BEAT;
export const DURATION = 25;
export const b = (n) => Math.round(n * BEAT * 1e4) / 1e4;

// Scene order, start beat, and the ledger vector each scene exits with.
export const SHOTS = [
  { id: 'world', start: 0, theme: 'paper' },
  { id: 'build', start: 8, theme: 'dark' },
  { id: 'organize', start: 16, theme: 'paper' },
  { id: 'cache', start: 24, theme: 'dark' },
  { id: 'play', start: 32, theme: 'paper' },
  { id: 'brand', start: 44, theme: 'dark' },
];

export const SEAMS = SHOTS.slice(1).map((shot, i) => {
  const from = SHOTS[i];
  const up = shot.id === 'brand';
  const vec = { axis: up ? 'y' : 'x', dir: -1 };
  return {
    id: `${from.id}-to-${shot.id}`,
    cut: b(shot.start),
    technique: up ? 'cut-the-curve UP — elevation into brand' : 'cut-the-curve LEFT',
    exit: { selector: `#shot-${from.id}`, ...vec, dur: 0.3, travel: 18 },
    entry: { selector: `#shot-${shot.id}`, ...vec, dur: 0.5, travel: 16 },
    blur: 8,
  };
});
