'use client';

import { AnimatePresence, motion, useReducedMotion } from 'motion/react';
import { useEffect, useState } from 'react';

export function HeroVerb({ verbs, className }: { verbs: string[]; className?: string }) {
  const reduce = useReducedMotion();
  const [index, setIndex] = useState(0);

  useEffect(() => {
    if (reduce) return;
    const id = setInterval(() => setIndex((i) => (i + 1) % verbs.length), 2600);
    return () => clearInterval(id);
  }, [reduce, verbs.length]);

  return (
    <span className={className}>
      <AnimatePresence mode="wait" initial={false}>
        <motion.span
          key={verbs[index]}
          className="inline-block"
          initial={reduce ? false : { y: '55%', opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          exit={reduce ? undefined : { y: '-55%', opacity: 0 }}
          transition={{ type: 'spring', stiffness: 320, damping: 32 }}
        >
          {verbs[index]}
        </motion.span>
      </AnimatePresence>
    </span>
  );
}
