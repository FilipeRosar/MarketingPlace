export interface MomentResponseDto {
  id: string;
  description: string;
  videoUrl: string;
  thumbUrl?: string | null;
  createdAt: Date;
}
