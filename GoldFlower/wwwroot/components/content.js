const AWAITINGUSERINPUT = 0;
const INSTALLING = 1;
const ABORTED = 2;
const FINISHED = 3;
const CANCELLED = 4;
const IDLE = 5;
const FAILED = 6;

export default {
	name: 'main-content',
	data() {
		return {
			path: window.path,
			files: [],
			selected: [],
			error: null,
			warning: null,
			events: [],
			installer: {
				status: IDLE,
				progress: 0,
				currentFile: null,
				files: [],
				events: null,
				processorType: 0
			},
			AWAITINGUSERINPUT: AWAITINGUSERINPUT,
			INSTALLING: INSTALLING,
			ABORTED: ABORTED,
			FINISHED: FINISHED,
			CANCELLED: CANCELLED,
			IDLE: IDLE,
			FAILED: FAILED,
			processors: {
				NONE: 0,
				TINFOIL: 1,
				GOLDLEAF: 2
			}
		};
	},
	template: `
		<div class="container-fluid mt--6" v-if="installer.processorType === processors.NONE">
			<div class="row">
				<div class="col-md-6 mb-6">
					<div class="card card-profile">
						<img src="/images/switch.jpg" alt="Image placeholder" class="card-img-top">
						<div class="card-img-overlay d-flex align-items-center">
							<div class="w-100">
								<h5 class="h2 card-title text-white text-shadow-black mb-2 text-center">Goldleaf</h5>
							</div>
						</div>
						<div class="row justify-content-center">
							<div class="col-lg-3 order-lg-2">
								<div class="card-profile-image">
									<a href="#" @click="setProcessorType(processors.GOLDLEAF)">
										<img src="/images/goldleaf.png" class="rounded-circle">
									</a>
								</div>
							</div>
						</div>
					</div>
				</div>

				<div class="col-md-6">
					<div class="card card-profile">
						<img src="/images/switch.jpg" alt="Image placeholder" class="card-img-top">						
						<div class="card-img-overlay d-flex align-items-center">
							<div class="w-100">
								<h5 class="h2 card-title text-white text-shadow-black mb-2 text-center">Tinfoil</h5>
							</div>
						</div>
						<div class="row justify-content-center">
							<div class="col-lg-3 order-lg-2">
								<div class="card-profile-image">
									<a href="#" @click="setProcessorType(processors.TINFOIL)">
										<img src="/images/empty.png" class="rounded-circle">
									</a>
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>

		<div class="container-fluid mt--6" v-else-if="installer.processorType !== processors.NONE">
			<div class="row">
				<div class="col">

					<div class="card bg-default">
						<!-- Card header -->
						<div class="card-header bg-default shadow">
							<h3 class="mb-0 text-white">Enter NSP directory</h3>
						</div>

						<!-- Card body -->
						<div class="card-body">							
							<div class="form-group">
								<div class="input-group input-group-merge">
									<div class="input-group-prepend">
										<span class="input-group-text"><i class="fa fa-file"></i></span>
									</div>
									<input class="form-control" placeholder="C:\\your\\directory" type="text" v-model="path">
								</div>
							</div>

							<div class="form-group mb-0">
								<button class="btn btn-secondary" @click="onSelectDirectoryClicked">Search directory</button>
							</div>
						</div>
					</div>
						
				</div>
			</div>

			<div class="row">
				<div class="col">
					<div class="card bg-default shadow">
						<div class="card-header bg-transparent border-0">
							<h3 class="text-white mb-0">NSP(s)</h3>
						</div>
						<div class="table-responsive">
							<table class="table align-items-center table-dark table-flush">
								<thead class="thead-dark">
									<tr>
										<th>Name</th>
										<th>Title ID</th>
										<th>Version</th>
										<th>Size</th>
										<th>Install</th>
										<th style="width: 75px;"></th>
									</tr>
								</thead>
								<tbody class="list">
									<tr v-for="(item, index) in files" :key="index">
										<th scope="row">
											<div class="media align-items-center">												
												<div class="media-body">
													<span class="name mb-0 text-sm">{{ item.name }}</span>
												</div>
											</div>
										</th>

										<td>
											{{ getTitleId(item.name) }}
										</td>

										<td>
											{{ getTitleVersion(item.name) }}
										</td>

										<td>
											{{ convertBytesToHumanSize(item.size) }}
										</td>
										
										<td class="text-center">
											<label class="custom-toggle-nsp custom-toggle custom-toggle-success">
												<input type="checkbox" @click="onFileClicked(item)" :checked="item.selected">
												<span class="custom-toggle-slider rounded-circle" data-label-off="" data-label-on=""></span>
											</label>
										</td>

										<td class="text-center" style="width: 50px;">
											<span class="badge badge-warning" v-if="item.selected">{{ selected.findIndex(x => x.name === item.name) + 1 }}</span>
										</td>
									</tr>
									<tr v-if="!files.length">
										<td colspan="6">
											No (.NSP) files found, make sure you search the correct directory.
										</td>
									</tr>
								</tbody>
							</table>							
						</div>
					</div>
				</div>
			</div>

			<div class="row">
				<div class="col">
					<div class="card bg-default shadow">
						<div class="card-header bg-transparent border-0">
							<h3 class="text-white mb-0">Installation</h3>
						</div>
						<div class="table-responsive">
							<table class="table align-items-center table-dark table-flush">
								<thead class="thead-dark">
									<tr>
										<th style="width: 75px;"></th>
										<th>Name</th>
										<th>Title ID</th>
										<th>Version</th>
										<th>Size</th>
										<th>Status</th>
										<th style="width: 75px;"></th>
									</tr>
								</thead>
								<tbody class="list">
									<tr v-for="(item, index) in installer.files" :key="index">
										<td>
											<i v-if="file.state === INSTALLING" class="text-white fa fa-spin fa-circle-o-notch"></i>
											<i v-else-if="file.state === FINISHED" class="text-white fa fa-check text-success"></i>
											<i v-else-if="file.state === FAILED" class="text-white fa fa-close text-danger"></i>
											<i v-else-if="file.state === CANCELLED" class="text-white fa fa-close text-danger"></i>
											<i v-else="file.state === IDLE" class="text-white fa fa-ellipsis-h"></i>
										</td>

										<th scope="row">
											<div class="media align-items-center">												
												<div class="media-body">
													<span class="name mb-0 text-sm">{{ item.name }}</span>
												</div>
											</div>
										</th>

										<td>
											{{ getTitleId(item.name) }}
										</td>

										<td>
											{{ getTitleVersion(item.name) }}
										</td>

										<td>
											{{ convertBytesToHumanSize(item.size) }}
										</td>
										
										<td class="text-center">
											<div class="d-flex align-items-center">
												<span class="completion mr-2">{{ installer.progress }}%</span>
												<div>
													<div class="progress">
														<div class="progress-bar bg-success" role="progressbar" :style="installerProgressWidth"></div>
													</div>
												</div>
											</div>
										</td>

										<td class="text-center" style="width: 50px;">
											<span class="badge badge-warning" v-if="item.selected">{{ selected.findIndex(x => x.name === item.name) + 1 }}</span>
										</td>
									</tr>
									<tr v-if="!files.length">
										<td colspan="6">
											No (.NSP) files found, make sure you search the correct directory.
										</td>
									</tr>
								</tbody>
							</table>							
						</div>
					</div>
				</div>
			</div>			
		</div>
	`,
	mounted() {
		if(this.path != null) {
			this.onSelectDirectoryClicked();
		}

		this.getInstallProgress();
	},
	methods: {
		onSelectDirectoryClicked() {
			this.files = [];
			this.error = null;
			this.warning = null;

			axios.post('installer/select-directory', { path: this.path })
				.then((response) => {
					if (response.data.result) {
						this.files = response.data.result;
					} else if (response.data.error) {
						this.error = response.data.error;
					}
				})
				.catch((error) => {
					this.error = error;
					console.error(error);
				});
		},

		translateProcessorType(type) {
			switch(type) {
				case 1: return 'Tinfoil';
				case 2: return 'Goldleaf';
				default: return '-';
			}
		},

		convertBytesToHumanSize(bytes, decimals) {
			if (bytes == 0) return '0 Bytes';

			let k = 1024,
				dm = decimals <= 0 ? 0 : decimals || 2,
				sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'],
				i = Math.floor(Math.log(bytes) / Math.log(k));

			return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
		},

		getTitleId(filename) {
			let regExp = /(?<=\[).+?(?=\])/g;
			let matches = filename.match(regExp);

			return matches && matches.length > 0 ? matches[0] : '-';
		},

		getTitleVersion(filename) {
			let regExp = /(?<=\[).+?(?=\])/g;
			let matches = filename.match(regExp);

			if (matches && matches.length > 1) {
				return matches[1].replace('v', '');
			}

			return '-';
		},

		onFileClicked(file) {
			if (!file.selected) {
				this.selected.push(file);
			} else {
				const index = this.selected.findIndex(x => x.name === file.name);
				this.selected.splice(index, 1);
			}

			file.selected = !file.selected;
		},

		onInstallCicked() {
			this.events = [];

			axios.post(`installer/install/${this.installer.processorType}`, this.selected)
				.then((response) => {
					if (response.data) {
						this.setInstaller(response.data);
						this.getInstallProgress();
					} else if (response.data.error) {
						this.error = response.data.error;
					}
				})
				.catch((error) => {
					this.error = error;
					console.error(error);
				});
		},

		getInstallProgress() {
			axios.get('installer/progress')
				.then((response) => {
					if (response.data) {
						this.setInstaller(response.data);

						if(this.installer.status === INSTALLING || this.installer.status === AWAITINGUSERINPUT) {
							setTimeout(() => {
								this.getInstallProgress();
							}, 1000);
						}

					} else if (response.data.error) {
						this.error = response.data.error;
					}
				})
				.catch((error) => {
					console.error(error);

					setTimeout(() => {
						this.getInstallProgress();
					}, 2500);
				});
		},

		setInstaller(data) {
			this.installer = data;

			for(let event of data.events) {
				if(!this.events.find(x => x.type === event.type && x.dateTime === event.dateTime)) {
					if (this.events.length >= 50) {
						this.events.splice(0, 1);
					}

					this.events.push(event);
				}
			}

			// Deselect installed
			if(data.files) {
				for(let file of data.files) {
					if(file.state === FINISHED) {
						let index = this.selected.findIndex(x => x.name === file.name);

						if(index > -1) {
							this.selected.splice(index, 1);
						}
					}
				}
			}

			this.warning = null;
			this.error = null;

			if (data.status === ABORTED) {
				this.error = 'Installation was aborted by the user';
			} else if(data.status === CANCELLED) {
				this.error = 'An error occured during installation';
			} else if (data.status === AWAITINGUSERINPUT) {
				switch(data.processorType) {
					case 0: 
						this.warning = '<p><strong>Awaiting user input</strong></p>Select <strong><i>Title management</i></strong> followed by <strong><i>USB installation</i></strong> in the Tinfoil app on the Switch';
						break;
					case 1:
						this.warning = '<p><strong>Awaiting user input</strong></p>Select <strong><i>USB installation</i></strong> in the Goldleaf app on the Switch<br />Select your prefered options in the Goldleaf app to start the installation';
						break;
				}
			}
		},

		setProcessorType(type) {
			this.events = [];

			axios.post(`installer/processorType/${type}`)
				.then((response) => {
					if (response.data) {
						this.installer.processorType = type;
						this.selected = [];
						this.$forceUpdate();
					} else if (response.data.error) {
						this.error = response.data.error;
					}
				})
				.catch((error) => {
					this.error = error;
					console.error(error);
				});
		},

		onAbortClicked() {
			if (confirm('Are you sure you want to abort the installation?')) {
				axios.post('installer/abort')
					.then((response) => {
						if (response.data) {
							this.setInstaller(response.data);
						} else if (response.data.error) {
							this.error = response.data.error;
						}
					})
					.catch((error) => {
						this.error = error;
						console.error(error);
					});
			}
		},

		onCompleteClicked() {
			this.events = [];

			axios.post('installer/complete')
				.then((response) => {
					if (response.data) {
						this.selected = [];
						this.setInstaller(response.data);
						this.$forceUpdate();
					} else if (response.data.error) {
						this.error = response.data.error;
					}
				})
				.catch((error) => {
					this.error = error;
					console.error(error);
				});
		}
	},
	computed: {
		installerProgress() {
			return this.installer.status === INSTALLING ? this.installer.files.findIndex(x => x.name === this.installer.currentFile.name) : 0;
		},

		installerProgressWidth() {
			return `width: {this.installer.progress}%;`;
		},

		sortedEvents() {
			return this.events.sort((a,b) => (a.dateTime < b.dateTime) ? 1 : ((b.dateTime < a.dateTime) ? -1 : 0));
		}
	}
};
