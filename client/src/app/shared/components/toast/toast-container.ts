import { Component, inject } from '@angular/core';
import { ToastService, ToastType } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  template: `
    <div class="fixed top-4 right-4 z-[9999] flex flex-col gap-3 pointer-events-none">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          [class]="'pointer-events-auto max-w-sm w-full px-4 py-3 rounded-xl shadow-lg border ' +
                   'flex items-start gap-3 animate-slide-in-right ' + typeClasses(toast.type)"
          role="alert">
          <span class="text-lg shrink-0 mt-0.5">{{ typeIcon(toast.type) }}</span>
          <p class="flex-1 text-sm font-medium leading-snug">{{ toast.message }}</p>
          <button
            (click)="toastService.dismiss(toast.id)"
            class="shrink-0 opacity-60 hover:opacity-100 transition-opacity cursor-pointer
                   bg-transparent border-none text-inherit text-lg leading-none"
            aria-label="Dismiss">&times;</button>
        </div>
      }
    </div>
  `,
})
export class ToastContainerComponent {
  protected readonly toastService = inject(ToastService);

  typeClasses(type: ToastType): string {
    const map: Record<ToastType, string> = {
      success: 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400',
      error: 'bg-red-500/10 border-red-500/20 text-red-400',
      warning: 'bg-amber-500/10 border-amber-500/20 text-amber-400',
      info: 'bg-blue-500/10 border-blue-500/20 text-blue-400',
    };
    return map[type];
  }

  typeIcon(type: ToastType): string {
    const map: Record<ToastType, string> = {
      success: '\u2705',
      error: '\u274C',
      warning: '\u26A0\uFE0F',
      info: '\u2139\uFE0F',
    };
    return map[type];
  }
}
