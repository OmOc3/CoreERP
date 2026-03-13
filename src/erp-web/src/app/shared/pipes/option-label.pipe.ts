import { Pipe, PipeTransform } from '@angular/core';
import { SelectOption } from '../../core/models/erp.enums';

@Pipe({
  name: 'optionLabel',
  standalone: true
})
export class OptionLabelPipe implements PipeTransform {
  transform(value: string | number | null | undefined, options: ReadonlyArray<SelectOption<string | number>>): string {
    return options.find((item) => item.value === value)?.label ?? String(value ?? '');
  }
}
