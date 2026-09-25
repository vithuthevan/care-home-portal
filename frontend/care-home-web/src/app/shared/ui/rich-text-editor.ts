import {
  AfterViewInit,
  Component,
  ElementRef,
  forwardRef,
  inject,
  viewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-rich-text-editor',
  imports: [MatButtonModule],
  template: `
    <div class="rich-text">
      <div class="rich-text__toolbar" role="toolbar" aria-label="Formatting">
        <button type="button" mat-stroked-button (mousedown)="$event.preventDefault()" (click)="command('bold')">
          Bold
        </button>
        <button type="button" mat-stroked-button (mousedown)="$event.preventDefault()" (click)="command('italic')">
          Italic
        </button>
        <button type="button" mat-stroked-button (mousedown)="$event.preventDefault()" (click)="command('underline')">
          Underline
        </button>
        <button
          type="button"
          mat-stroked-button
          (mousedown)="$event.preventDefault()"
          (click)="command('insertUnorderedList')"
        >
          List
        </button>
      </div>
      <div
        #editor
        class="rich-text__body"
        contenteditable="true"
        role="textbox"
        aria-multiline="true"
        (input)="onInput()"
        (blur)="onTouched()"
      ></div>
    </div>
  `,
  styles: `
    .rich-text__toolbar {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-bottom: 0.5rem;
    }
    .rich-text__body {
      min-height: 7rem;
      padding: 0.75rem;
      border: 1px solid var(--app-border);
      border-radius: 0.5rem;
      background: var(--app-surface, transparent);
      line-height: 1.45;
    }
    .rich-text__body:focus {
      outline: 2px solid var(--app-focus, #6750a4);
      outline-offset: 1px;
    }
  `,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RichTextEditorComponent),
      multi: true,
    },
  ],
})
export class RichTextEditorComponent implements ControlValueAccessor, AfterViewInit {
  private readonly host = inject(ElementRef);
  private readonly editor = viewChild<ElementRef<HTMLDivElement>>('editor');
  private pending = '';
  private onChange: (value: string) => void = () => undefined;
  onTouched: () => void = () => undefined;

  ngAfterViewInit(): void {
    this.writeHtml(this.pending);
  }

  writeValue(value: string | null): void {
    this.pending = value ?? '';
    this.writeHtml(this.pending);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    const editor = this.editor()?.nativeElement;
    if (editor) {
      editor.contentEditable = isDisabled ? 'false' : 'true';
    }
    this.host.nativeElement.toggleAttribute('inert', isDisabled);
  }

  command(name: string): void {
    this.editor()?.nativeElement.focus();
    document.execCommand(name);
    this.onInput();
  }

  onInput(): void {
    const html = this.editor()?.nativeElement.innerHTML ?? '';
    this.pending = html;
    this.onChange(html);
  }

  private writeHtml(value: string): void {
    const editor = this.editor()?.nativeElement;
    if (editor && editor.innerHTML !== value) {
      editor.innerHTML = value;
    }
  }
}
