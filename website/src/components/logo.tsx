export function Logo(props: React.ComponentProps<'svg'>) {
  return (
    <svg viewBox="0 0 128 128" fill="none" xmlns="http://www.w3.org/2000/svg" {...props}>
      <g transform="translate(64 64) rotate(45) scale(0.95)">
        <rect
          x="-30"
          y="-30"
          width="60"
          height="60"
          rx="16"
          stroke="currentColor"
          strokeWidth="12"
          transform="translate(-17 17)"
        />
        <rect
          x="-30"
          y="-30"
          width="60"
          height="60"
          rx="16"
          stroke="currentColor"
          strokeWidth="12"
          transform="translate(17 -17)"
        />
      </g>
    </svg>
  );
}
