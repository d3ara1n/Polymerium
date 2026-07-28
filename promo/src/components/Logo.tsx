import React from "react";
import { C } from "../theme";

// Polymerium logo: two rounded squares, rotated 45 degrees, offset diagonally.
// `shift` animates the two halves sliding together (0 = apart, 1 = assembled).
export const Logo: React.FC<{
  size?: number;
  color?: string;
  shift?: number;
  opacity?: number;
  style?: React.CSSProperties;
}> = ({ size = 128, color = C.primary, shift = 1, opacity = 1, style }) => {
  const apart = 1 - shift;
  return (
    <svg
      viewBox="0 0 128 128"
      width={size}
      height={size}
      fill="none"
      style={{ opacity, overflow: "visible", ...style }}
    >
      <g transform="translate(64 64) rotate(45) scale(0.95)">
        <rect
          x="-30"
          y="-30"
          width="60"
          height="60"
          rx="16"
          stroke={color}
          strokeWidth="12"
          transform={`translate(${-17 - apart * 26} ${17 + apart * 26})`}
        />
        <rect
          x="-30"
          y="-30"
          width="60"
          height="60"
          rx="16"
          stroke={color}
          strokeWidth="12"
          transform={`translate(${17 + apart * 26} ${-17 - apart * 26})`}
        />
      </g>
    </svg>
  );
};
