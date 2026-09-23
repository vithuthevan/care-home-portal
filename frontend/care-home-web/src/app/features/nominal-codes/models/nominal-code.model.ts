import { MasterDataUsage } from '../../../shared/format/master-data-usage';

export interface NominalCode {
  id: number;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
  configurationSource?: string;
  usage?: MasterDataUsage | null;
}

export interface CreateNominalCodeRequest {
  code: string;
  name: string;
  description?: string;
}

export interface UpdateNominalCodeRequest extends CreateNominalCodeRequest {
  isActive: boolean;
}
