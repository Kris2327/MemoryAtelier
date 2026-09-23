import { Component, Input, forwardRef, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-password-field',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './password-field.html',
  styleUrl: './password-field.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PasswordField),
      multi: true
    }
  ]
})
export class PasswordField implements ControlValueAccessor {
  @Input() placeholder = '';
  @Input() minlength: number | null = null;

  value = '';
  visible = signal(false);
  disabled = false;

  private onChange: (value: string) => void = () => {};
  onTouched: () => void = () => {};

  writeValue(value: string): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onInput(value: string): void {
    this.value = value;
    this.onChange(value);
  }

  toggleVisible(): void {
    this.visible.update(v => !v);
  }
}
