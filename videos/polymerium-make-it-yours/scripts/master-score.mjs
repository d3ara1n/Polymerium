import fs from 'node:fs';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
const root=path.resolve(import.meta.dirname,'..');
function ff(args){const r=spawnSync('ffmpeg',['-hide_banner',...args],{cwd:root,encoding:'utf8'});if(r.status!==0)throw new Error(r.stderr);return r.stderr;}
const analysis=ff(['-i','out/score-premaster.wav','-af','loudnorm=I=-13.7:TP=-1.6:LRA=8:print_format=json','-f','null','-']);
const a=JSON.parse(analysis.match(/\{\s*"input_i"[\s\S]*?\}/)[0]);
const filter=`loudnorm=I=-13.7:TP=-1.6:LRA=8:measured_I=${a.input_i}:measured_TP=${a.input_tp}:measured_LRA=${a.input_lra}:measured_thresh=${a.input_thresh}:offset=${a.target_offset}:linear=true:print_format=json`;
const log=ff(['-y','-i','out/score-premaster.wav','-af',filter,'-ar','48000','-c:a','pcm_s24le','out/score.wav']);
fs.mkdirSync(path.join(root,'renders'),{recursive:true});fs.writeFileSync(path.join(root,'renders/loudness-master.log'),log);console.log('Mastered original score to out/score.wav.');
